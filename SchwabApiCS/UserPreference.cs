// <copyright file="UserPreference.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. http://mozilla.org/MPL/2.0/.
// </copyright>

using System;

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        public UserPreferences GetUserPreferences()
        {
            return WaitForCompletion(GetUserPreferencesAsync());
        }

        public async Task<ApiResponseWrapper<UserPreferences>> GetUserPreferencesAsync()
        {
            return await Get<UserPreferences>(AccountsBaseUrl + "/userPreference");
        }

        public class UserPreferences
        {
            public List<Account> accounts { get; set; }
            public List<StreamerInfo> streamerInfo { get; set; }
            public List<Offer> offers { get; set; }

            public class Account
            {
                public string accountNumber { get; set; }
                public bool primaryAccount { get; set; }
                public string type { get; set; }
                public string nickName { get; set; }
                public string displayAcctId { get; set; }
                public bool autoPositionEffect { get; set; }
                public string accountColor { get; set; }

                public override string ToString()
                {
                    return accountNumber + " " + nickName + " " + type;
                }
            }

            public class Offer
            {
                public bool level2Permissions { get; set; }
                public string mktDataPermission { get; set; }
            }

            public class StreamerInfo
            {
                public string streamerSocketUrl { get; set; }
                public string schwabClientCustomerId { get; set; }
                public string schwabClientCorrelId { get; set; }
                public string schwabClientChannel { get; set; }
                public string schwabClientFunctionId { get; set; }
            }
        }

    }

}


