///////////////////////////////////////////////////////////////////////
// Display.cs - Displays the Type Analysis and Relationship          //
//                            Analysis table                         //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Dell Precision T7400, Win7, SP1                      //
// Application: Demonstration for CSE681, Project #2, Fall 2011      //
// Author:      Smruti Tatavarthy                                    //
//                                                                   //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Displays the data from the location Table 1 containing the Type Analysis
 * Displays the data from the relations table containing the relationship Analysis
 * Displays the corresponding file name for every table
 *  Displays the output in XML format and writes into an XML file
*/

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Xml;
using System.IO;


namespace CodeAnalysis
{
    public class Display
    {
        static void Main()
        {
            Display dis = new Display();
            dis.displayPackage(true);
        }

        // this method is to display the Type and Function Analysis
        public void displayPackage(bool showRelation)
        {
            Pass1Repository pass1Rep = Pass1Repository.getInstance();
            List<Elem> table = pass1Rep.locations1;           
            bool functionHeader = true;
            string previousFileName = "";
            bool tableFlag = true;
            string funcComplexity = "";

            int fileCount = 0;

            Elem temp = new Elem();

            foreach (Elem e in table)
            {
                if (e.functionComplexity == 0)
                {
                    funcComplexity = "-";
                }
                else
                {
                    funcComplexity = e.functionComplexity.ToString();
                }
                
                if (functionHeader)
                {

                    Console.WriteLine("\n\n\n Processing file : " + e.fileName);
                    Console.WriteLine("\n\n  TYPE AND FUNCTION ANALYSIS");
                    Console.WriteLine("---------------------------------------------------------------------------------------------");
                    Console.WriteLine("\n  {0,8}  {1,22} {2,12} {3,8}  {4,10} {5,15}", "TYPE", "NAME", "BEGIN", "END", "SIZE", "FUNCTION COMPLEXITY");
                    Console.Write("\n");
                    if (temp.name != null)
                    {
                        Console.WriteLine("{0,10} {1,25} {2,8} {3,10}  {4,10} {5,12}", temp.type, temp.name, temp.begin, temp.end, temp.size, funcComplexity);
                        temp = new Elem();
                    }
                }
                functionHeader = false;
                
                if (e.fileName == previousFileName || tableFlag)
                 {
                     Console.WriteLine("{0,10} {1,25} {2,8} {3,10}  {4,10} {5,12}", e.type, e.name, e.begin, e.end, e.size, funcComplexity);
                    previousFileName = e.fileName;
                }
                
                tableFlag = false;
                
                if ((e.fileName != previousFileName))
                {
                    if (showRelation)
                    {
                        Pass2Repository pass2Rep = Pass2Repository.getInstance();
                        List<Elem> relTable = pass2Rep.locations2;
                        printRelationsTable(relTable, previousFileName);                        
                    }
                    previousFileName = e.fileName;
                    functionHeader = true;
                    fileCount = fileCount + 1;

                    temp.baseClass = e.baseClass;
                    temp.begin = e.begin;
                    temp.type = e.type;
                    temp.end = e.end;
                    temp.size = e.size;
                    temp.name = e.name;
                    temp.functionComplexity = e.functionComplexity;
                }                               
            }
            if (showRelation)
            {
                Pass2Repository pass2Rep1 = Pass2Repository.getInstance();
                List<Elem> relTable1 = pass2Rep1.locations2;
                printRelationsTable(relTable1, previousFileName);
            }            
        }
        // this method is to display the Relationships
        public void printRelationsTable(List<Elem> relTable, string fileToPrint)
        {
            bool headerFlag = true;
            for (int i = 0; i < relTable.Count; i++)
            {
                if (headerFlag)
                {
                    Console.Write("\n\n RELATIONSHIP ANALYSIS");
                    Console.Write("\n ---------------------------------------------------------------------------------------------");
                    Console.Write("\n {0,20}  {1,20}  {2,27}", " CLASS NAME", "RELATIONSHIP", "CLASS NAME");
                }
                headerFlag = false;
                if (relTable[i].relationship != null && (relTable[i].fileName == fileToPrint))
                {
                    Console.Write("\n {0,20} {1,20} {2,27}", relTable[i].baseClass, relTable[i].relationship, relTable[i].name);
                } 
            }
            Console.Write("\n\n\n");
        }

        // this method is to write the Code Analyzer output into an XML file
        public void writeToXML(bool showRelation, string path)
        {
            Pass1Repository pass1Rep = Pass1Repository.getInstance();
            List<Elem> table = pass1Rep.locations1;

            Pass2Repository pass2Rep = Pass2Repository.getInstance();

            List<Elem> relTable = pass2Rep.locations2;

            string previousFileName = "";
            bool tableFlag = true;

            int fileCount = 0;

            XDocument xml = new XDocument();
            xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            XComment comment = new XComment("Printing Parser Results into XML format");
            xml.Add(comment);

            XElement codeAnalyzer = new XElement("CodeAnalyzer");
            xml.Add(codeAnalyzer);

            XElement file = new XElement("File");           
            XElement typeFunctionAnalysis = new XElement("TypeFunctionAnalysis");

            Elem temp = new Elem();
                       
            foreach (Elem e in table)
            {
                XElement elementTemp = new XElement("Element");
                XElement typeNameTemp = new XElement("TypeName", temp.type);
                XElement classNameTemp = new XElement("ClassName", temp.name);
                XElement sizeTemp = new XElement("Size", temp.size);
                XElement complexityTemp = new XElement("Complexity", temp.functionComplexity);

                elementTemp.Add(typeNameTemp);
                elementTemp.Add(classNameTemp);
                elementTemp.Add(sizeTemp);
                elementTemp.Add(complexityTemp);

                if (temp.type != null) 
                    typeFunctionAnalysis.Add(elementTemp);

                temp = new Elem();

                if (e.fileName == previousFileName || tableFlag)
                {                    
                    XElement element = new XElement("Element");
                    XElement typeName = new XElement("TypeName", e.type);
                    XElement className = new XElement("ClassName", e.name);
                    XElement size = new XElement("Size", e.size);
                    XElement complexity = new XElement("Complexity", e.functionComplexity);
                    element.Add(typeName);
                    element.Add(className);
                    element.Add(size);
                    element.Add(complexity);
                    typeFunctionAnalysis.Add(element);
                    previousFileName = e.fileName;
                }

                tableFlag = false;

                if (e.fileName != previousFileName)
                {
                    file = new XElement("File");
                    file.SetAttributeValue("FileName", previousFileName);

                    if (showRelation)
                    {
                        XElement relationAnalysis = new XElement("RelationAnalysis");

                        for (int i = 0; i < relTable.Count; i++)
                        {
                            if (relTable[i].relationship != null && (relTable[i].fileName == previousFileName))
                            {
                                XElement element = new XElement("Element");
                                
                                XElement className1 = new XElement("ClassName1", relTable[i].baseClass);
                                XElement relationship = new XElement("Relation", relTable[i].relationship);
                                XElement className2 = new XElement("ClassName2", relTable[i].name);

                                element.Add(className1);
                                element.Add(relationship);
                                element.Add(className2);
                                relationAnalysis.Add(element);

                                temp.baseClass = e.baseClass;
                                temp.begin = e.begin;
                                temp.type = e.type;
                                temp.end = e.end;
                                temp.size = e.size;
                                temp.name = e.name;
                                temp.functionComplexity = e.functionComplexity;
                                
                            }                            
                        }

                        file.Add(relationAnalysis);                                                
                    }

                    previousFileName = e.fileName;
                    fileCount = fileCount + 1;                   

                    file.Add(typeFunctionAnalysis);
                    codeAnalyzer.Add(file);
                     typeFunctionAnalysis = new XElement("TypeFunctionAnalysis");
                }
                
            }

            XElement relationAnalysis1 = null;

            if (showRelation)
            {

                relationAnalysis1 = new XElement("RelationAnalysis"); 

                for (int i = 0; i < relTable.Count; i++)
                {
                    if (relTable[i].relationship != null && (relTable[i].fileName == previousFileName))
                    {
                        XElement className1 = new XElement("ClassName1", relTable[i].baseClass);
                        XElement relationship = new XElement("Relation", relTable[i].relationship);
                        XElement className2 = new XElement("ClassName2", relTable[i].name);
                        XElement element = new XElement("Element");
                        element.Add(className1);
                        element.Add(relationship);
                        element.Add(className2);
                        relationAnalysis1.Add(element);
                    }
                }
            }


            file = new XElement("File");
            file.SetAttributeValue("FileName", previousFileName);
            file.Add(typeFunctionAnalysis);
            if (relationAnalysis1 != null)
            {
                file.Add(relationAnalysis1);
            }
            
            
            codeAnalyzer.Add(file);

            Console.Write("\n OUTPUT IN XML FORMAT");
            Console.Write("\n --------------------------------------------------------------------------------------------- \n\n");
            Console.Write("\n{0}\n", xml.Declaration);
            Console.Write(xml.ToString());
            Console.Write("\n\n");

            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xml.ToString());
            xdoc.Save(path + "CodeAnalysis_Output.xml");
            Console.Write("\n File \"CodeAnalysis_Output.xml\"  saved in path - " + Path.GetFullPath(path));
            Console.Write("\n\n");

        }
    }
}

