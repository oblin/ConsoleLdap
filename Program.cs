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

            Console.WriteLine("Example 1. Search uid=yiming with attribute:");
            var username = "yiming";
            var userpwd = "l123456";
            // filter 範例：
            // POSIX 表達式：
            // & and： (&(Kim Smith) (telephonenumber=555-5555)) 
            // ｜ or：  (|(cn=Kim Smith)(cn=Kimberly Smith))
            // ！ not： (!(cn=Kim Smith)) 
            var searchFilter = $"(uid={username})";     // 單一條件使用 uid=yiming 也是一樣
            string[] requiredAttributes = { "cn", "uid", "mail", "manager", "gecos" };

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

                try
                {
                    // 直接使用 GetAttribute 有風險，因為並非每一個 DN 都有此 attribute，因此需要用 catch 避免中斷程式
                    var account = entity.GetAttribute("uid");
                    if (account != null && account.StringValue == username)
                    {
                        userdn = entity.Dn;
                        break;
                    }
                }
                catch(LdapException ex)
                {
                    Console.WriteLine($"dn: {entity.Dn} has exception: {ex.LdapErrorMessage}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"dn: {entity.Dn} has exception: {ex.Message}");
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

            Console.WriteLine("Example 2. Add userr Smith with password: l123456");

            connection.Bind(binddn, bindpwd);

            string containerName = "ou=IT,ou=ABL,dc=ablh,dc=com,dc=tw";
            string uid = "e11001";
            LdapAttributeSet attributeSet = new LdapAttributeSet();
		    attributeSet.Add(new LdapAttribute( "objectclass", "inetOrgPerson"));                
            attributeSet.Add(new LdapAttribute("cn", new string[]{"James Smith", "Jim Smith", "Jimmy Smith"}));               
			attributeSet.Add(new LdapAttribute("givenname", "James"));        
		    attributeSet.Add(new LdapAttribute("sn", "Smith"));        
		    attributeSet.Add(new LdapAttribute("uid", uid));        
		    attributeSet.Add(new LdapAttribute("telephonenumber","1 801 555 1212"));                                                     
		    attributeSet.Add(new LdapAttribute("mail", "JSmith@ablh.com.tw"));
		    attributeSet.Add(new LdapAttribute("userpassword","l123456"));  

            var dn = $"uid={uid}," + containerName;
            LdapEntry newEntry = new LdapEntry(dn, attributeSet);

            try
            {
                connection.Add(newEntry);
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"Add entry {dn} Error:" + ex.LdapErrorMessage);
            }

            connection.Disconnect();
        }
    }
}
