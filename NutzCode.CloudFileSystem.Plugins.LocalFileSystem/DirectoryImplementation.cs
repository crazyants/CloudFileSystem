﻿using System;
using System.Collections.Generic;

using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using DirectoryInfo = Pri.LongPath.DirectoryInfo;
using File = Pri.LongPath.File;
using FileSystemInfo = Pri.LongPath.FileSystemInfo;
using FileInfo = Pri.LongPath.FileInfo;
using Stream = System.IO.Stream;
using FileAttributes = System.IO.FileAttributes;

namespace NutzCode.CloudFileSystem.Plugins.LocalFileSystem
{
    public abstract class  DirectoryImplementation : LocalObject, IDirectory, IOwnDirectory<LocalFile,DirectoryImplementation>
    {
        public List<DirectoryImplementation> IntDirectories { get; set; }=new List<DirectoryImplementation>();
        public List<LocalFile> IntFiles { get; set; }=new List<LocalFile>();


        public List<IDirectory> Directories => IntDirectories.Cast<IDirectory>().ToList();
        public List<IFile> Files => IntFiles.Cast<IFile>().ToList();

        public abstract void CreateDirectory(string name);
        public abstract DirectoryInfo[] GetDirectories();
        public abstract FileInfo[] GetFiles();


        public DirectoryImplementation(LocalFileSystem fs) : base(fs)
        {
            
        }

        public virtual async Task<FileSystemResult<IFile>> CreateFileAsync(string name, Stream readstream, CancellationToken token, IProgress<FileProgress> progress, Dictionary<string, object> properties)
        {
            return await InternalCreateFile(this, name, readstream, token, progress, properties);
        }

        public virtual async Task<FileSystemResult<IDirectory>> CreateDirectoryAsync(string name, Dictionary<string, object> properties)
        {
            try
            {
                if (properties == null)
                    properties = new Dictionary<string, object>();
                CreateDirectory(name);
                DirectoryInfo dinfo = new DirectoryInfo(Path.Combine(FullName, name));
                if (properties.Any(a => a.Key.Equals("ModifiedDate", StringComparison.InvariantCultureIgnoreCase)))
                    dinfo.LastWriteTime = (DateTime)properties.First(a => a.Key.Equals("ModifiedDate", StringComparison.InvariantCultureIgnoreCase)).Value;
                if (properties.Any(a => a.Key.Equals("CreatedDate", StringComparison.InvariantCultureIgnoreCase)))
                    dinfo.CreationTime = (DateTime)properties.First(a => a.Key.Equals("CreatedDate", StringComparison.InvariantCultureIgnoreCase)).Value;
                LocalDirectory f = new LocalDirectory(dinfo,FS);
                f.Parent = this;
                FS.Refs[f.FullName] = f;
                IntDirectories.Add(f);
                return await Task.FromResult(new FileSystemResult<IDirectory>(f));

            }
            catch (Exception e)
            {
                return new FileSystemResult<IDirectory>("Error : " + e.Message);
            }
        }

        public virtual bool IsPopulated { get; internal set; }
        public bool IsRoot { get; internal set; } = false;

        public virtual async Task<FileSystemResult> PopulateAsync()
        {
            IntDirectories = GetDirectories().Select(a => new LocalDirectory(a,FS) {Parent = this}).Cast<DirectoryImplementation>().ToList();
            IntDirectories.ForEach(a=>FS.Refs[a.FullName]=a);
            IntFiles = GetFiles().Select(a => new LocalFile(a,FS) { Parent=this }).ToList();
            IsPopulated = true;
            return await Task.FromResult(new FileSystemResult());
        }

        //TODO Mono Implementation
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);



        public virtual async Task<FileSystemResult<FileSystemSizes>> QuotaAsync()
        {
            FileSystemSizes Sizes = new FileSystemSizes();
            ulong freebytes;
            ulong totalnumberofbytes;
            ulong totalnumberoffreebytes;
            if (GetDiskFreeSpaceEx(FullName, out freebytes, out totalnumberofbytes, out totalnumberoffreebytes))
            {
                Sizes.TotalSize = (long)totalnumberoffreebytes;
                Sizes.AvailableSize = (long) freebytes;
                Sizes.UsedSize = Sizes.TotalSize - Sizes.AvailableSize;
            }
            return await Task.FromResult(new FileSystemResult<FileSystemSizes>(Sizes));
        }
    }
}
