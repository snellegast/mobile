﻿using static Android.App.Assist.AssistStructure;
using Android.App.Assist;
using Bit.App;
using System.Collections.Generic;

namespace Bit.Android.Autofill
{
    public class Parser
    {
        public static HashSet<string> TrustedBrowsers = new HashSet<string>
        {
            "org.mozilla.focus","org.mozilla.firefox","org.mozilla.firefox_beta","com.microsoft.emmx",
            "com.android.chrome","com.chrome.beta","com.android.browser","com.brave.browser","com.opera.browser",
            "com.opera.browser.beta","com.opera.mini.native","com.chrome.dev","com.chrome.canary",
            "com.google.android.apps.chrome","com.google.android.apps.chrome_dev","com.yandex.browser",
            "com.sec.android.app.sbrowser","com.sec.android.app.sbrowser.beta","org.codeaurora.swe.browser",
            "com.amazon.cloud9","org.mozilla.klar"
        };

        private readonly AssistStructure _structure;
        private string _uri;
        private string _packageName;
        private string _webDomain;

        public Parser(AssistStructure structure)
        {
            _structure = structure;
        }

        public FieldCollection FieldCollection { get; private set; } = new FieldCollection();
        public string Uri
        {
            get
            {
                if(!string.IsNullOrWhiteSpace(_uri))
                {
                    return _uri;
                }

                if(string.IsNullOrWhiteSpace(WebDomain) && string.IsNullOrWhiteSpace(PackageName))
                {
                    _uri = null;
                }
                else if(!string.IsNullOrWhiteSpace(WebDomain))
                {
                    _uri = string.Concat("http://", WebDomain);
                }
                else
                {
                    _uri = string.Concat(Constants.AndroidAppProtocol, PackageName);
                }

                return _uri;
            }
        }
        public string PackageName
        {
            get => _packageName;
            set
            {
                if(string.IsNullOrWhiteSpace(value))
                {
                    _packageName = _uri = null;
                }

                _packageName = value;
            }
        }
        public string WebDomain
        {
            get => _webDomain;
            set
            {
                if(string.IsNullOrWhiteSpace(value))
                {
                    _webDomain = _uri = null;
                }

                _webDomain = value;
            }
        }

        public void Parse()
        {
            for(var i = 0; i < _structure.WindowNodeCount; i++)
            {
                var node = _structure.GetWindowNodeAt(i);
                ParseNode(node.RootViewNode);
            }
        }

        private void ParseNode(ViewNode node)
        {
            var hints = node.GetAutofillHints();
            var isEditText = node.ClassName == "android.widget.EditText" || node?.HtmlInfo?.Tag == "input";
            if(isEditText || (hints?.Length ?? 0) > 0)
            {
                if(PackageName == null)
                {
                    PackageName = node.IdPackage;
                }
                if(WebDomain == null && TrustedBrowsers.Contains(node.IdPackage))
                {
                    WebDomain = node.WebDomain;
                }

                FieldCollection.Add(new Field(node));
            }
            else
            {
                if(WebDomain == null && TrustedBrowsers.Contains(node.IdPackage))
                {
                    WebDomain = node.WebDomain;
                }

                FieldCollection.IgnoreAutofillIds.Add(node.AutofillId);
            }

            for(var i = 0; i < node.ChildCount; i++)
            {
                ParseNode(node.GetChildAt(i));
            }
        }
    }
}