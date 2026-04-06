using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

string docxPath = @"C:\Users\user\Desktop\Авы\Курсач читы 26.docx";
string outputPath = @"C:\Users\user\source\repos\OrgTechRepair\курсач_текст.txt";

try
{
    using (var archive = ZipFile.OpenRead(docxPath))
    {
        var documentEntry = archive.GetEntry("word/document.xml");
        if (documentEntry != null)
        {
            using (var stream = documentEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                var xml = reader.ReadToEnd();
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                
                var nsManager = new XmlNamespaceManager(doc.NameTable);
                nsManager.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                
                var textNodes = doc.SelectNodes("//w:t", nsManager);
                var sb = new StringBuilder();
                
                if (textNodes != null)
                {
                    foreach (XmlNode node in textNodes)
                    {
                        sb.Append(node.InnerText);
                    }
                }
                
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine("Текст успешно извлечен!");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
