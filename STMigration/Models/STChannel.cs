﻿// Copyright (c) Isak Viste. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable IDE1006

namespace STMigration.Models {
    public class STChannel {
        #region Properties

        public string displayName { get; set; }
        public string description { get; set; }
        public string createdDateTime { get; set; }
        public string membershipType { get; set; } = "standard";

        #endregion
        #region Constructors

        public STChannel(string displayName, string description, string createdDateTime) {
            this.displayName = displayName;
            this.description = description;
            this.createdDateTime = createdDateTime;
        }

        public STChannel(string dirName, string createdDateTime) {
            displayName = dirName;
            description = $"Description for {dirName}";
            this.createdDateTime = createdDateTime;
        }

        #endregion
    }
}
