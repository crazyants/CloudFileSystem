// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace NutzCode.CloudFileSystem.Plugins.OneDrive.Models
{
    /// <summary>
    /// The type ItemReference.
    /// </summary>
    [DataContract]
    public class ItemReference
    {
    
        /// <summary>
        /// Gets or sets driveId.
        /// </summary>
        [DataMember(Name = "driveId", EmitDefaultValue = false, IsRequired = false)]
        public string DriveId { get; set; }
    
        /// <summary>
        /// Gets or sets id.
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false, IsRequired = false)]
        public string Id { get; set; }
    
        /// <summary>
        /// Gets or sets path.
        /// </summary>
        [DataMember(Name = "path", EmitDefaultValue = false, IsRequired = false)]
        public string Path { get; set; }
    
        /// <summary>
        /// Gets or sets additional data.
        /// </summary>
        [JsonExtensionData(ReadData = true)]
        public IDictionary<string, object> AdditionalData { get; set; }
    
    }
}
