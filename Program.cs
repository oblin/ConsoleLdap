using Novell.Directory.Ldap;
using System;

namespace ConsoleLdap
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var host = "192.168.1.106";
            var binddn = "cn=admin,dc=ablh,dc=com,dc=tw";
            var bindpwd = "l490910";
            var basedc = "dc=ablh,dc=com,dc=tw";

            using var connection = new LdapConnection();
            connection.Connect(host, LdapConnection.DefaultPort);
            connection.Bind(binddn, bindpwd);

            var username = "yiming";
            var userpwd = "l123456";
            var searchFilter = $"uid={username}";
            string[] requiredAttributes = { "cn", "uid", "mail" };

            // SCOPE_BASE 只查詢目前 DN 的項目
            // SCOPE_ONE 只查詢目前 DN 及下層 DN 的項目
            // SCOPE_SUB 查詢目前 DN 下的樹（通常會使用此設定）
            // entities => LdapSearchResults
            // requiredAttributes => null 代表讀回全部 attributes
            var entities = connection.Search(basedc, LdapConnection.ScopeSub, searchFilter, requiredAttributes, false);
            
            string userdn = null;
            while(entities.HasMore())
            {
                LdapEntry entity = entities.Next();
                Console.WriteLine($"entity dn: {entity.Dn}");
                LdapAttributeSet attrSet = entity.GetAttributeSet();
                System.Collections.IEnumerator enumerator = attrSet.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    LdapAttribute attribute = (LdapAttribute)enumerator.Current;
                    string attributeName = attribute.Name;
                    string attributeValue = attribute.StringValue;
                    Console.WriteLine($"\tattribute: {attributeName} value: {attributeValue}");
                }

                var account = entity.GetAttribute("uid");
                if (account != null && account.StringValue == username)
                {
                    userdn = entity.Dn;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(userdn))
                Console.WriteLine("Not found");
            try
            {
                connection.Bind(userdn, userpwd);
                Console.WriteLine($"ldap auth result: {connection.Bound}");
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"user {username} connect bind failed, exception: {ex.LdapErrorMessage}");
            }

            connection.Disconnect();
        }
    }
}
