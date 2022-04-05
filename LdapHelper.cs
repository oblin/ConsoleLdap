using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleLdap
{
    public class LdapHelper
    {
        string host = "192.168.1.113";
        string binddn = "cn=admin,dc=ablh,dc=com,dc=tw";
        string bindpwd = "l490910";
        string _basedc;

        private readonly LdapConnection _adminconnection;

        public LdapHelper(string basedc)
        {
            _basedc = basedc;
            _adminconnection = new LdapConnection();
            _adminconnection.Connect(host, LdapConnection.DefaultPort);
            _adminconnection.Bind(binddn, bindpwd);
        }

        public LdapConnection BindAdmin()
        {
            _adminconnection.Bind(binddn, bindpwd);
            return _adminconnection;
        }

        public LdapConnection AdminConnection => _adminconnection;

        public LdapEntry FindDn(string dn)
        {
            LdapEntry entry = null;
            try
            {
                // 透過 Read 可以直接讀取 ldap object，不需要再用 Search 方式
                entry = _adminconnection.Read(dn);
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"Read dn: {dn} Failed: {ex.LdapErrorMessage}");
            }
            return entry;
        }
    }
}
