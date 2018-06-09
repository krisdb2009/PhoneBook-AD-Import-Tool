using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PhoneBookImport
{
    class ConfigGenerator
    {
        public ConfigGenerator()
        {
            StreamWriter file = File.CreateText("PhoneBookImport.exe.config");
            file.Write(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<configuration>\n" +
                "  <appSettings>\n" +
                "    <add key=\"ldapSearchRoot\" value=\"OU=Human Resources,DC=ad,DC=contoso,DC=com\" />\n" +
                "    <add key=\"ldapFilter\" value=\"(&amp;(objectClass=user)(!(objectClass=computer)))\" />\n" +
                "    <add key=\"ldapNumberAttribute\" value=\"telephone\" />\n" +
                "    <add key=\"API\" value=\"https://phonebook.contoso.com/api/\" />\n" +
                "    <add key=\"descriptionString\" value=\"%givenName% %sn% - %title%\" />\n" +
                "    <add key=\"tagList\" value=\"givenName, sn, department, company\" />\n" +
                "    <add key=\"translationList\" value=\"hr=human resources,it=information technology,tech=technician\" />\n" +
                "  </appSettings>\n" +
                "</configuration>\n"
            );
            file.Close();
        }
    }
}
