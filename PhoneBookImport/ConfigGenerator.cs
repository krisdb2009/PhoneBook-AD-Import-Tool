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
                "    <!-- An LDAP Distinguished Name of a container in which the import will run -->\n" +
                "    <add key=\"ldapSearchRoot\" value=\"OU=Human Resources,DC=ad,DC=contoso,DC=com\" />\n" +
                "    <!-- An LDAP filter for matching objects in a directory to be imported into the Phone Book -->\n" +
                "    <add key=\"ldapFilter\" value=\"(&amp;(objectClass=user)(!(objectClass=computer)))\" />\n" +
                "    <!-- The AD attribute of the telephone number -->\n" +
                "    <add key=\"ldapNumberAttribute\" value=\"telephone\" />\n" +
                "    <!-- The PhoneBook API URL -->\n" +
                "    <add key=\"API\" value=\"https://phonebook.contoso.com/api/\" />\n" +
                "    <!-- \n" +
                "    The description format that will be imported into the phone book.\n" +
                "    Text in between percent symbols will be interpreted as an LDAP attribute and will be replaced with the attributes value.\n" +
                "    -->\n" +
                "    <add key=\"descriptionString\" value=\"%givenName% %sn% - %title%\" />\n" +
                "    <!-- A list of tags seperated by a comma that will be converted into tags in the Phone Book -->\n" +
                "    <add key=\"tagList\" value=\"givenName, sn, department, company\" />\n" +
                "    <!-- \n" +
                "    A list of values that will be translated.\n" +
                "    For example: In the below config, any tag from an attribute above that becomes the text \"HR\" will be tranlated into two other tags \"human\" and \"resources\"\n" +
                "    -->\n" +
                "    <add key=\"translationList\" value=\"hr=human resources,it=information technology,tech=technician\" />\n" +
                "  </appSettings>\n" +
                "</configuration>\n"
            );
            file.Close();
        }
    }
}
