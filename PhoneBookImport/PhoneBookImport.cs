using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Configuration;
using System.IO;
using System.Net;
using System.Collections.Specialized;

namespace PhoneBookImport
{
    class PhoneBookImport
    {
        static List<List<string>> translation = new List<List<string>>();
        static List<string> descriptionScheme = new List<string>();
        static List<string> ldapTags = new List<string>();
        static DirectorySearcher dSearch;
        static string ldapNumberAttribute;
        static string API;

        static void Main(string[] args)
        {
            Console.WriteLine("Running as: " + Environment.UserDomainName + "\\" + Environment.UserName);
            Console.WriteLine("Reading configuration...");
            init();
            Console.WriteLine("Running directory search on: " + DomainController.FindOne(new DirectoryContext(DirectoryContextType.Domain)).Name + "...");

            //Run a domain search.
            SearchResultCollection results = dSearch.FindAll();

            //Generate json string from the search results.
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
            Console.WriteLine("\nJSON to be uploaded:\n" + jsonNumbers + "\n");

            //Upload results to the API.
            Console.WriteLine("Uploading json...");
            uploadResults(jsonNumbers);
            Console.WriteLine("Done.");
        }

        //Uploads a json string to the API.
        public static void uploadResults(string json)
        {
            WebClient client = new WebClient();
            client.UseDefaultCredentials = true;
            var values = new NameValueCollection();
            values["api"] = "import";
            values["import"] = json;
            client.UploadValues(API, "POST", values);
        }

        //Generates a phone number from a search result, and clears any characters that are not numbers.
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
                        num = num.Replace(character.ToString(), "");
                    }
                }
                number = num;
            }
            return number;
        }

        //Generates a description from a search result and then escapes any quotes.
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
            return description.Replace("\"", "\\\"");
        }

        //Initializes the configuration and varaibles.
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
            API = ConfigurationManager.AppSettings["API"];
            string descriptionString = ConfigurationManager.AppSettings["descriptionString"];
            string tagList = ConfigurationManager.AppSettings["tagList"];
            string translationList = ConfigurationManager.AppSettings["translationList"];

            //Setup the directory searcher using the filter specified in the config.
            dSearch = new DirectorySearcher(ConfigurationManager.AppSettings["ldapFilter"]);

            //Parse the list of tags.
            foreach (string tag in tagList.Split(char.Parse(",")))
            {
                string ldapAttr = tag.TrimStart(char.Parse(" "));
                if (!ldapTags.Contains(ldapAttr))
                {
                    dSearch.PropertiesToLoad.Add(ldapAttr);
                    ldapTags.Add(ldapAttr);
                }
            }

            //Parse the description scheme.
            int lineCount = 0;
            foreach (string scheme in descriptionString.Split(char.Parse("%")))
            {
                if (lineCount++ % 2 == 1 && !scheme.Contains(" "))
                {
                    dSearch.PropertiesToLoad.Add(scheme);
                }
                descriptionScheme.Add(scheme);
            }

            //Parse the translation list.
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
                PhoneBookImport.translation.Add(list);
            }

            //Set directory search parameters.
            dSearch.PageSize = 10000;
            dSearch.PropertiesToLoad.Add(ldapNumberAttribute);
            dSearch.SearchRoot = new DirectoryEntry("LDAP://" + ConfigurationManager.AppSettings["ldapSearchRoot"]);
        }

        //Generates tags from a search result, and then returns a list.
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

        //Translates a a tag into another tag based on the translation list.
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