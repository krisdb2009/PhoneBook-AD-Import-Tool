using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace PhoneBookImport
{
    class Router
    {
        static List<List<string>> translation = new List<List<string>>();
        static void Main(string[] args)
        {
            //If configuration does not exits, create one.
            if (!File.Exists("PhoneBookImport.exe.config"))
            {
                new ConfigGenerator();
                return;
            }

            //Pull configuration.
            string ldapNumberAttribute = ConfigurationManager.AppSettings["ldapNumberAttribute"];
            string API = ConfigurationManager.AppSettings["API"];
            string descriptionString = ConfigurationManager.AppSettings["descriptionString"];
            string tagList = ConfigurationManager.AppSettings["tagList"];
            string translationList = ConfigurationManager.AppSettings["translationList"];

            //Setup the directory searcher using the filter specified in the config.
            DirectorySearcher dSearch = new DirectorySearcher(ConfigurationManager.AppSettings["ldapFilter"]);

            List<string> tags = new List<string>();

            foreach (string tag in tagList.Split(char.Parse(",")))
            {
                string ldapAttr = tag.TrimStart(char.Parse(" "));
                if(!tags.Contains(ldapAttr))
                {
                    dSearch.PropertiesToLoad.Add(ldapAttr);
                    tags.Add(ldapAttr);
                }
            }
            int lineCount = 0;
            foreach (string ldapAttr in descriptionString.Split(char.Parse("%")))
            {
                if (lineCount++ % 2 == 1 && !ldapAttr.Contains(" "))
                {
                    dSearch.PropertiesToLoad.Add(ldapAttr);
                }
            }
            foreach (string translation in translationList.Split(char.Parse(",")))
            {
                string[] split = translation.Split(char.Parse("="));
                List<string> list = new List<string>();
                list.Add(split[0]);
                foreach (string tag in split[1].Split(char.Parse(" ")))
                {
                    if (tag != "")
                    {
                        list.Add(tag);
                    }
                }
                Router.translation.Add(list);
            }

            dSearch.PageSize = 10000;
            dSearch.PropertiesToLoad.Add(ldapNumberAttribute);
            dSearch.SearchRoot = new DirectoryEntry("LDAP://" + ConfigurationManager.AppSettings["ldapSearchRoot"]);
            SearchResultCollection results = dSearch.FindAll();
            foreach (SearchResult result in results)
            {
                if (result.Properties[ldapNumberAttribute].Count > 0)
                {
                    Console.WriteLine("\n" + result.Path);
                    //Console.WriteLine(result.Properties[ldapNumberAttribute][0] + "\n");

                    foreach (string t in tags)
                    {
                        if(result.Properties[t].Count != 0)
                        {
                            string tagString = result.Properties[t][0].ToString();
                            foreach (char character in tagString)
                            {
                                if (!char.IsLetter(character))
                                {
                                    tagString = tagString.Replace(character, char.Parse(" "));
                                }
                            }
                            foreach(string tag in tagString.Split(char.Parse(" ")))
                            {
                                if(tag != "")
                                {
                                    foreach(string translatedTag in tagTranslator(tag))
                                    {
                                        Console.WriteLine(translatedTag);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.ReadLine();
        }

        public static List<string> tagTranslator(string tag)
        {
            List<string> translatedTags = new List<string>();
            bool found = false;
            foreach(List<string> list in translation)
            {
                if(list[0].ToLower() == tag.ToLower())
                {
                    found = true;
                    foreach(string replacementTag in list)
                    {
                        if(replacementTag.ToLower() != tag.ToLower())
                        {
                            translatedTags.Add(replacementTag);
                        }
                    }
                }
            }
            if(!found)
            {
                translatedTags.Add(tag);
            }
            return translatedTags;
        }
    }
}