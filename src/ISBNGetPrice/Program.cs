using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ISBNGetPrice
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string[] isbnList = File.ReadAllLines(args[0]);
            //string[] isbnList = File.ReadAllLines(@"C:/porch_isbn.csv");
            Dictionary<string, string> prices = new Dictionary<string, string>();
            int count = 1;

            foreach (string isbn in isbnList)
            {
                string price = string.Empty;
                Console.WriteLine($"{count}:");
                if (isbn.Length == 13)
                {
                    try
                    {
                        Console.WriteLine($"Getting details for ISBN {isbn}");
                        string url = $"http://www.google.com/books/feeds/volumes/?q=ISBN%3C{isbn}%3E";
                        Console.WriteLine(url);
                        WebRequest request = WebRequest.Create(url);
                        Task<WebResponse> responseTask = request.GetResponseAsync();
                        responseTask.Wait();
                        Stream responseStream = responseTask.Result.GetResponseStream();

                        XmlDocument doc = new XmlDocument();
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                        nsmgr.AddNamespace("bk", "http://www.w3.org/2005/Atom");
                        nsmgr.AddNamespace("dc", "http://purl.org/dc/terms");
                        nsmgr.AddNamespace("gbs", "http://schemas.google.com/books/2008");
                        nsmgr.AddNamespace("gd", "http://schemas.google.com/g/2005");
                        doc.Load(responseStream);
                        XmlNode root = doc.DocumentElement;

                        foreach (XmlNode book in root.SelectNodes("descendant::bk:entry", nsmgr))
                        {
                            if (price == string.Empty)
                            {
                                foreach (XmlNode identifier in book.SelectNodes("dc:identifier", nsmgr))
                                {
                                    if (identifier.InnerText.Contains(isbn))
                                    {
                                        XmlNode priceSuggestedRetail = book.SelectSingleNode("gbs:price[@type='SuggestedRetailPrice']", nsmgr);
                                        if (priceSuggestedRetail != null)
                                        {
                                            XmlAttributeCollection priceAttribute = priceSuggestedRetail.SelectSingleNode("gd:money", nsmgr).Attributes;
                                            price = priceAttribute["amount"].Value;
                                            Console.WriteLine($"Suggested Retail Price = ${price}");
                                        }
                                    }
                                    if (identifier.InnerText.Contains(isbn) && price == string.Empty)
                                    {
                                        XmlNode priceRetail = book.SelectSingleNode("gbs:price[@type='RetailPrice']", nsmgr);
                                        if (priceRetail != null)
                                        {
                                            XmlAttributeCollection priceAttribute = priceRetail.SelectSingleNode("gd:money", nsmgr).Attributes;
                                            price = priceAttribute["amount"].Value;
                                            Console.WriteLine($"Retail Price = ${price}");
                                        }
                                    }
                                    if (identifier.InnerText.Contains(isbn) && price == string.Empty)
                                    {
                                        Console.WriteLine("No price available");
                                    }
                                }
                            }
                        }

                        responseStream.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    Thread.Sleep(200);
                }
                if (!prices.ContainsKey(isbn) && price != string.Empty) prices.Add(isbn, price);

                count++;
                Console.WriteLine();
            }

            List<string> priceOutput = new List<string>();
            foreach (KeyValuePair<string, string> price in prices)
            {
                priceOutput.Add($"{price.Key},{price.Value}");
            }
            File.WriteAllLines("output.csv", priceOutput.ToArray());
            Console.WriteLine("Saved as output.csv");
            Console.ReadKey();
        }
    }
}
