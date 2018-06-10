using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Configuration;
using System.IO;

namespace PhoneBookImport
{
    class Router
    {
        static List<List<string>> translation = new List<List<string>>();
        static List<string> descriptionScheme = new List<string>();
        static List<string> ldapTags = new List<string>();
        static DirectorySearcher dSearch;
        static string ldapNumberAttribute;

        static void Main(string[] args)
        {
            Console.WriteLine("Reading configuration...");
            init();
            Console.WriteLine("Running directory search on: " + DomainController.FindOne(new DirectoryContext(DirectoryContextType.Domain)).Name + "...");
            SearchResultCollection results = dSearch.FindAll();
            string jsonNumbers = "{";
            bool first = true;
            foreach (SearchResult result in results)
            {
                if (result.Properties[ldapNumberAttribute].Count > 0)
                {
                    string json = "\"";
                    string number = generatePhoneNumber(result);
                    string description = generateDescription(result);
                    Console.WriteLine("\nDistinguished Name: " + result.Path);
                    Console.WriteLine("Phone Number: " + number);
                    Console.WriteLine("Generated Description: " + description);
                    Console.WriteLine("Generated Tags:");
                    json = json + number + "\":{\"description\":\"" + description + "\",\"tags\":[";
                    List<string> tags = generateTags(result);
                    string jsonTags = "";
                    int count = 0;
                    foreach (string tag in tags)
                    {
                        Console.WriteLine(tag);
                        jsonTags = jsonTags + "\"" + tag + "\"";
                        if(count++ != tags.Count - 1)
                        {
                            jsonTags = jsonTags + ",";
                        }
                    }
                    json = json + jsonTags + "]}";
                    if(first)
                    {
                        first = false;
                    }
                    else
                    {
                        json = "," + json;
                    }
                    jsonNumbers = jsonNumbers + json;
                }
            }
            jsonNumbers = jsonNumbers + "}";

            Console.WriteLine(jsonNumbers);
            Console.ReadLine();
        }

        public static string generatePhoneNumber(SearchResult searchResult)
        {
            string number = "";
            if(searchResult.Properties[ldapNumberAttribute].Count != 0)
            {
                string num = searchResult.Properties[ldapNumberAttribute][0].ToString();
                foreach (char character in num)
                {
                    if (!char.IsNumber(character))
                    {
                        num = num.Replace(character, char.Parse(""));
                    }
                }
                number = num;
            }
            return number;
        }

        public static string generateDescription(SearchResult searchResult)
        {
            int count = 0;
            string description = "";
            foreach(string scheme in descriptionScheme)
            {
                if(count++ % 2 == 1)
                {
                    if(searchResult.Properties[scheme].Count != 0)
                    {
                        description = description + searchResult.Properties[scheme][0].ToString();
                    }
                }
                else
                {
                    description = description + scheme;
                }
            }
            string escapeQuote(string s)
            {
                count = 0;
                foreach (char character in s)
                {
                    if (character == char.Parse("\""))
                    {
                        if (count == 0 || s[count - 1] != char.Parse("\\"))
                        {
                            s = s.Insert(count, "\\");
                            s = escapeQuote(s);
                            break;
                        }
                    }
                    count++;
                }
                return s;
            }
            return escapeQuote(description);
        }

        public static void init()
        {
            //If configuration does not exits, create one.
            if (!File.Exists("PhoneBookImport.exe.config"))
            {
                new ConfigGenerator();
                return;
            }

            //Pull configuration.
            ldapNumberAttribute = ConfigurationManager.AppSettings["ldapNumberAttribute"];
            string API = ConfigurationManager.AppSettings["API"];
            string descriptionString = ConfigurationManager.AppSettings["descriptionString"];
            string tagList = ConfigurationManager.AppSettings["tagList"];
            string translationList = ConfigurationManager.AppSettings["translationList"];

            //Setup the directory searcher using the filter specified in the config.
            dSearch = new DirectorySearcher(ConfigurationManager.AppSettings["ldapFilter"]);

            foreach (string tag in tagList.Split(char.Parse(",")))
            {
                string ldapAttr = tag.TrimStart(char.Parse(" "));
                if (!ldapTags.Contains(ldapAttr))
                {
                    dSearch.PropertiesToLoad.Add(ldapAttr);
                    ldapTags.Add(ldapAttr);
                }
            }
            int lineCount = 0;
            foreach (string scheme in descriptionString.Split(char.Parse("%")))
            {
                if (lineCount++ % 2 == 1 && !scheme.Contains(" "))
                {
                    dSearch.PropertiesToLoad.Add(scheme);
                }
                descriptionScheme.Add(scheme);
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
        }

        public static List<string> generateTags(SearchResult searchResult)
        {
            List<string> finalTags = new List<string>();
            foreach (string t in ldapTags)
            {
                if (searchResult.Properties[t].Count != 0)
                {
                    string tagString = searchResult.Properties[t][0].ToString();
                    foreach (char character in tagString)
                    {
                        if (!char.IsLetter(character))
                        {
                            tagString = tagString.Replace(character, char.Parse(" "));
                        }
                    }
                    foreach (string tag in tagString.Split(char.Parse(" ")))
                    {
                        if (tag != "")
                        {
                            foreach (string translatedTag in tagTranslator(tag))
                            {
                                if (!finalTags.Contains(translatedTag))
                                {
                                    finalTags.Add(translatedTag);
                                }
                            }
                        }
                    }
                }
            }
            return finalTags;
        }

        public static List<string> tagTranslator(string tag)
        {
            List<string> translatedTags = new List<string>();
            bool found = false;
            foreach(List<string> list in translation)
            {
                string lowerTag = tag.ToLower();
                if(list[0].ToLower() == lowerTag)
                {
                    found = true;
                    bool first = true;
                    foreach(string replacementTag in list)
                    {
                        if (!first)
                        {
                            translatedTags.Add(replacementTag.ToLower());
                        }
                        else
                        {
                            first = false;
                        }
                    }
                }
            }
            if(!found)
            {
                translatedTags.Add(tag.ToLower());
            }
            return translatedTags;
        }
    }
}