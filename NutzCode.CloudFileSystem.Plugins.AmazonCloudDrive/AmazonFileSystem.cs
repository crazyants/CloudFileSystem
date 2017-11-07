﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NutzCode.CloudFileSystem.OAuth2;



namespace NutzCode.CloudFileSystem.Plugins.AmazonCloudDrive
{
    public class AmazonFileSystem : AmazonRoot, IFileSystem
    {

        internal const string AmazonOAuth = "https://api.amazon.com/auth/o2/token";
        internal const string AmazonEndpoint = "https://drive.amazonaws.com/drive/v1/account/endpoint";
        internal const string AmazonOAuthLogin = "https://www.amazon.com/ap/oa";
        internal const string AmazonQuota = "{0}/account/quota";

        internal static List<string> AmazonScopes = new List<string> { "clouddrive:read_all", "clouddrive:write" };
        internal string AppFriendlyName { get; set; }
        internal OAuth OAuth;
        //internal WeakReferenceContainer Refs=new WeakReferenceContainer();

        internal DirectoryCache.DirectoryCache Refs=new DirectoryCache.DirectoryCache(CloudFileSystemPluginFactory.DirectoryTreeCacheSize);

        public FileSystemResult<IObject> ResolveSynchronous(string path)
        {
            return ResolveAsync(path).Result;
        }

        public SupportedFlags Supports => SupportedFlags.Assets | SupportedFlags.MD5 | SupportedFlags.Properties;

        private AmazonFileSystem(IOAuthProvider provider) : base(null)
        {
            FS = this;
            OAuth=new OAuth(provider);
        }

        internal async Task<FileSystemResult> CheckExpirations()
        {
            FileSystemResult r = await OAuth.MayRefreshToken();
            if (!r.IsOk)
                return r;
            r = await OAuth.MayRefreshEndPoint();
            return r;
        }



        public string GetUserAuthorization()
        {
            AuthorizationData dta=new AuthorizationData();
            dta.Token = OAuth.Token;
            dta.EndPoint = OAuth.EndPoint;
            return dta.Serialize();
        }

        public async Task<FileSystemResult<IObject>> ResolveAsync(string path)
        {
            return await Refs.ObjectFromPath(this, path) ?? new FileSystemResult<IObject>("Not found");
        }

        public FileSystemSizes Sizes { get; private set; }

        public static async Task<FileSystemResult<AmazonFileSystem>> Create(string fname, IOAuthProvider provider, Dictionary<string,object> settings, string pluginanme, string userauthorization=null)
        {
            AmazonFileSystem am=new AmazonFileSystem(provider);
            am.FS = am;
            am.OAuth.OAuthUrl = AmazonOAuth;
            am.OAuth.EndPointUrl = AmazonEndpoint;
            am.OAuth.OAuthLoginUrl = AmazonOAuthLogin;
            am.OAuth.DefaultScopes = AmazonScopes;
            bool userauth = !string.IsNullOrEmpty(userauthorization);
            if (userauth)
                am.DeserializeAuth(userauthorization);
            FileSystemResult r = await am.OAuth.Login(settings, pluginanme, userauth,false);
            if (!r.IsOk)
                return new FileSystemResult<AmazonFileSystem>(r.Error);
            r = await am.CheckExpirations();
            if (!r.IsOk)
                return new FileSystemResult<AmazonFileSystem>(r.Error);
            string url = AmazonRoot.FormatRest(am.OAuth.EndPoint.MetadataUrl);
            FileSystemResult<dynamic> fr = await am.List(url);
            if (!fr.IsOk)
                return new FileSystemResult<AmazonFileSystem>(fr.Error);
            foreach (dynamic v in fr.Result)
            {
                if (v.kind == "FOLDER")
                {
                    am.SetData(JsonConvert.SerializeObject(v));
                    am.FsName = fname;
                    await am.PopulateAsync();
                    return new FileSystemResult<AmazonFileSystem>(am);
                }
            }
            return new FileSystemResult<AmazonFileSystem>("Amazon Root directory not found");
        }

        public new async Task<FileSystemResult<FileSystemSizes>> QuotaAsync()
        {
            string url = AmazonQuota.FormatRest(OAuth.EndPoint.MetadataUrl);
            FileSystemResult<Json.Quota> cl = await FS.OAuth.CreateMetadataStream<Json.Quota>(url);
            if (!cl.IsOk)
                return new FileSystemResult<FileSystemSizes>(cl.Error);
            Sizes = new FileSystemSizes
            {
                AvailableSize = cl.Result.available,
                TotalSize = cl.Result.quota,
                UsedSize = cl.Result.quota - cl.Result.available
            };
            return new FileSystemResult<FileSystemSizes>(Sizes);
        }


        public void DeserializeAuth(string auth)
        {
            AuthorizationData d = AuthorizationData.Deserialize(auth);
            OAuth.Token = d.Token;
            OAuth.EndPoint = d.EndPoint;
        }


    }
}
