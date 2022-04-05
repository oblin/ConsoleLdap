using Novell.Directory.Ldap;
using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace ConsoleLdap
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            string basedc = "dc=ablh,dc=com,dc=tw";
            var ldapHelper = new LdapHelper(basedc);
            var connection = ldapHelper.AdminConnection;

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
            //var entities = connection.Search(basedc, LdapConnection.ScopeSub, searchFilter, null, false);
            
            string userdn = null;
            while(entities.HasMore())
            {
                LdapEntry entity = entities.Next();
                Console.WriteLine($"entity dn: {entity.Dn}");
                LdapAttributeSet attrSet = entity.GetAttributeSet();
                IEnumerator enumerator = attrSet.GetEnumerator();
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

            connection = ldapHelper.BindAdmin();

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
            attributeSet.Add(new LdapAttribute("userpassword", "l123456"));

            var dn = $"uid={uid}," + containerName;
            var entry = ldapHelper.FindDn(dn);
            if (entry == null)
            {
                LdapEntry newEntry = new LdapEntry(dn, attributeSet);
                try
                {
                    connection.Add(newEntry);
                }
                catch (LdapException ex)
                {
                    Console.WriteLine($"Add entry {dn} Error: {ex.Message}, Ldap Error: {ex.LdapErrorMessage}");
                }
            }
            else
            {
                //Console.WriteLine($"dn: {dn} had already exist, proceed modification");
                //ArrayList modList = new ArrayList();

                //// Add new Value of.
                //LdapAttribute attr = new LdapAttribute("description", "modification description");
                //modList.Add(new LdapModification(LdapModification.Add, attr));

                //// Modify value
                //attr = new LdapAttribute("mail", "james_smith@ablh.com.tw");
                //modList.Add(new LdapModification(LdapModification.Replace, attr));

                //LdapModification[] mods = new LdapModification[modList.Count];
                ////Type type = Type.GetType("Novell.Directory.Ldap.LdapModification");
                //mods = (LdapModification[])modList.ToArray(typeof(LdapModification));
                //connection.Modify(dn, mods);

                connection.Delete(dn);
            }

            Console.WriteLine("Example 3. Add organization Unit");
            containerName = "ou=ABL,dc=ablh,dc=com,dc=tw";
            string ou = "財會部";
            dn = $"ou={ou}," + containerName;
            attributeSet = new LdapAttributeSet();
            attributeSet.Add(new LdapAttribute("objectclass", "organizationalUnit"));
            entry = ldapHelper.FindDn(dn);
            if (entry == null)
            {
                LdapEntry newEntry = new LdapEntry(dn, attributeSet);
                try
                {
                    connection.Add(newEntry);
                }
                catch (LdapException ex)
                {
                    Console.WriteLine($"Add entry {dn} Error: {ex.Message}, Ldap Error: {ex.LdapErrorMessage}");
                }
            }

            Console.WriteLine("Example 4. change password");
            dn = "uid=yiming,ou=IT,ou=ABL,dc=ablh,dc=com,dc=tw";
            LdapAttribute attributePassword = new LdapAttribute("userPassword",
                "l490910");
            connection.Modify(dn, new LdapModification(LdapModification.Replace, attributePassword));


            Console.WriteLine("Example 5. Verify password");
            // 以下方式不行，主要原因是因為資料庫目前是存加密後的 {SHA} 密碼，無法跟明碼比對
            //attributePassword = new LdapAttribute("userPassword", "l123456");
            //bool correct = connection.Compare(dn, attributePassword);
            //Console.WriteLine(correct ? "The password: l123456 is correct." : "The password l123456 is incorrect.\n");
            try
            {
                connection.Bind(dn, "l123456");
                Console.WriteLine($"password l123456 verify: {connection.Bound}");
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"user {username} connect bind failed, exception: {ex.LdapErrorMessage}");
            }

            try
            {
                connection.Bind(dn, "l490910");
                Console.WriteLine($"password l490910 verify: {connection.Bound}");
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"user {username} connect bind failed, exception: {ex.LdapErrorMessage}");
            }

            connection.Disconnect();
        }
    }
}
