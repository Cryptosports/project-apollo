﻿//   Copyright 2020 Vircadia
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;

using Newtonsoft.Json;

using Project_Apollo.Entities;
using Project_Apollo.Registry;
using System.Data.Common;
using Microsoft.VisualBasic.CompilerServices;
using NUnit.Framework.Constraints;
using System.Text.RegularExpressions;

namespace Project_Apollo.Hooks
{
    public class PaginationInfo
    {
        private static readonly string _logHeader = "[PaginationInfo]";

        // Overall parameters
        private readonly int _pageNum = 1;   // The number of page to return
        private readonly int _perPage = 20;  // How many entries per page

        // Results from last filter() operation
        int _currentPage = 1;
        int _currentItem = 1;

        public PaginationInfo(RESTRequestData pReq)
        {
            try
            {
                if (pReq.Queries.TryGetValue("per_page", out string perPageString))
                {
                    _perPage = Tools.Clamp(Int32.Parse(perPageString), 1, 1000);
                }
                if (pReq.Queries.TryGetValue("page", out string pageString))
                {
                    _pageNum = Tools.Clamp(Int32.Parse(pageString), 1, 1000);
                }
            }
            catch (Exception e)
            {
                Context.Log.Error("{0} Exception fetching parameters: {1}", _logHeader, e);
            }
        }

        /// <summary>
        /// Given an Enumeratable of some items, return an enumerable which
        /// is the paged portion of that list.
        /// </summary>
        /// <typeparam name="T">Type of enumerable to scan and to return</typeparam>
        /// <param name="pToFilter">An enumerable of things to paginate</param>
        /// <returns></returns>
        public IEnumerable<T> Filter<T>(IEnumerable<T> pToFilter)
        {
            _currentPage = 1;
            _currentItem = 1;
            foreach (T item in pToFilter)
            {
                if (_pageNum == _currentPage)
                {
                    yield return item;
                }
                if (++_currentItem > _perPage) {
                    _currentItem = 1;
                    if (++_currentPage > _pageNum)
                    {
                        // If we're past the requested page, we're done
                        break;
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// The HTML reply body can have extra top level fields which
        ///     describe what pagination happened.
        /// </summary>
        /// <param name="pBody"></param>
        public void AddReplyFields(ResponseBody pBody)
        {
        }
    }

    public class AccountFilterInfo
    {
        private static readonly string _logHeader = "[AccountFilterInfo]";
        readonly string _filter; // one of 'connections','friends',
        readonly string _status; // one of 'online',
        readonly string _search; // specific name to look for (wildcards?)
        public AccountFilterInfo(RESTRequestData pReq)
        {
            try
            {
                pReq.Queries.TryGetValue("filter", out _filter);
                pReq.Queries.TryGetValue("status", out _status);
                pReq.Queries.TryGetValue("search", out _search);
            }
            catch (Exception e)
            {
                Context.Log.Error("{0} Exception fetching parameters: {1}", _logHeader, e);
            }
        }

        /// <summary>
        /// Generate an enumerable of filtered accounts.
        /// </summary>
        /// <param name="pMustBeOnline">if 'true', all returned accounts are 'online'</param>
        /// <param name="pRequestingAcct">requesting account. Used for admin permissions and connective checks.
        ///     Optional. If not specified, connectivity will be false.</param>
        /// <returns></returns>
        public IEnumerable<AccountEntity> Filter(AccountEntity pRequestingAcct = null)
        {
            // Don't do any filtering yet
            foreach (AccountEntity acct in Accounts.Instance.AllAccountEntities()) {
                bool matched = false;

                if (!matched && !String.IsNullOrEmpty(_filter))
                {
                    string[] pieces = _filter.Split(",");
                    foreach (var filterCheck in pieces)
                    {
                        if (matched) break;

                        switch (filterCheck)
                        {
                            case "online":
                                if (acct.IsOnline)
                                {
                                    matched = true;
                                }
                                break;
                            case "friends":
                                if (acct.IsFriend(pRequestingAcct))
                                {
                                    matched = true;
                                }
                                break;
                            case "connections":
                                if (acct.IsConnected(pRequestingAcct))
                                {
                                    matched = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (!matched && !String.IsNullOrEmpty(_status))
                {
                    string[] pieces = _status.Split(",");
                    foreach (var statusCheck in pieces)
                    {
                        if (matched) break;

                        switch (statusCheck)
                        {
                            case "online":
                                if (acct.IsOnline)
                                {
                                    matched = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (!matched && !String.IsNullOrEmpty(_search))
                {
                    // TODO: does this do wildcard things?
                    if (_search == acct.Username)
                    {
                        matched = true;
                    }
                }
                if (matched) yield return acct;
            }
            yield break;
        }
    }

    /// <summary>
    /// Filter for an enumerable of AccountEntities.
    /// Normally, a user can only see a collection of accounts that they
    /// are connected to. This filter has logic for that and for other
    /// scope information.
    /// </summary>
    public class AccountScopeFilter
    {
        private static readonly string _logHeader = "[AccountScopeInfo]";
        AccountEntity _contextAccount;
        public AccountScopeFilter(AccountEntity pAccount)
        {
            _contextAccount = pAccount;
        }

        /// <summary>
        /// Generate an enumerable of filtered accounts.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AccountEntity> Filter(IEnumerable<AccountEntity> pAccounts)
        {
            // Don't do any filtering yet
            foreach (AccountEntity acct in pAccounts) {
                bool matched = false;
                if (_contextAccount != null)
                {
                    if (_contextAccount.IsAdmin)
                    {
                        matched = true;
                    }
                    else
                    {
                        if (_contextAccount.IsConnected(acct))
                        {
                            matched = true;
                        }
                    }
                }
                if (matched) yield return acct;
            }
            yield break;
        }
    }

    public class APIQuery
    {
    }
}
