///////////////////////////////////////////////////////////////////////
// Analyzer.cs - Manages Code Analysis                               //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Dell Precision T7400, Win7, SP1                      //
// Application: Demonstration for CSE681, Project #2, Fall 2011      //
// Author:      Smruti Tatavarthy                                    //
//                                                                   //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Manages Code Analysis
 * Finds all the files from subdirectories and passes them file
 * by file to 2 passes.
 * The first pass calls the parser for Type Analysis of the code
 * The second pass call the parser for Relationship Analysis of the code
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis
{
    public class Analyzer
    {        
        
        static public string[] getFiles(string path, List<string> patterns, bool subDir)
        {
            FileMgr fm = new CodeAnalysis.FileMgr();
            foreach (string pattern in patterns)
            {
                fm.addPattern(pattern);
            }

            fm.findFiles(path, subDir);
            return fm.getFiles().ToArray();
        }
         public static void doAnalysis(List<string> files)
        {           
           Pass1Repository pass1Rep = new Pass1Repository();
           Console.Write("\n ------------------------------------FILE SUMMARY------------------------------------------------\n");
            foreach (object file in files)
            {
                Console.Write("\n File {0}\n", file as string);

                CSsemi.CSemiExp semi = new CSsemi.CSemiExp();
                semi.displayNewLines = false;
                if (!semi.open(file as string))
                {
                    Console.Write("\n  Can't open {0}\n\n", file);
                    return;
                }
                BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
                Parser parser = builder.build();
                try
                {
                    while (semi.getSemi())
                    {
                        parser.parse(semi);                       
                    }                        
                }
                catch (Exception ex)
                {
                    Console.Write("\n\n  {0}\n", ex.Message);                    
                }
               Repository rep = Repository.getInstance();               
                bool printFileName = true;               
                for (int i=0; i < rep.locations.Count; i++) 
                {                    
                    rep.locations[i].fileName = file as string;
                    if (printFileName)
                    { 
                    rep.locations[i].textToPrint = "\n\n\n Processing File {0}\n" + file as string;
                    printFileName = false;
                    }

                    if (rep.locations[i].type == "function")
                    {
                        rep.locations[i].functionComplexity = rep.locations[i].functionComplexity + 1;
                    }                                        
                    pass1Rep.locations1.Add(rep.locations[i]);                
                }               
                semi.close();                
            }
        }
         public static void doRelationshipAnalysis(List<string> files)
         {
             Pass2Repository pass2Rep = new Pass2Repository();
             foreach (object file in files)
             {
                 CSsemi.CSemiExp semi = new CSsemi.CSemiExp();
                 semi.displayNewLines = false;
                 if (!semi.open(file as string))
                 {
                     Console.Write("\n  Can't open {0}\n\n", file);
                     return;
                 }
                 BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
                 ParserPass2 parser2 = builder.build2();
                 try
                 {
                     while (semi.getSemi())
                     {
                         parser2.parse(semi);
                     }

                 }
                 catch (Exception ex)
                 {
                     Console.Write("\n\n  {0}\n", ex.Message);
                 }
                 Repository rep = Repository.getInstance();
                 for (int i = 0; i < rep.relLocations.Count; i++)
                 {
                     rep.relLocations[i].fileName = file as string;
                     pass2Rep.locations2.Add(rep.relLocations[i]);
                 }
                 semi.close();
             }
         }
        static void Main(string[] args)
        {
            string path = args[0];
            bool subDir = false;
            string rel = args[2];           
            List<string> patterns = new List<string>();
            patterns.Add("*.cs");
            patterns.Add("*.h");
            patterns.Add("*.cpp");
            string[] files = Analyzer.getFiles(path,patterns,subDir);
            List<string> listOfFiles = new List<string>(files);
            listOfFiles.Sort();
            for (int j=0; j < listOfFiles.Count; j++)
            {
                if (listOfFiles[j].Contains("TemporaryGeneratedFile")) 
                {
                    listOfFiles.RemoveAt(j);
                    j = j - 1;
                }
            }            
            doAnalysis(listOfFiles);

            SecondClass sc = new SecondClass();
        }
    }

    public class SecondClass
    {
        public void printSomeThing()
        {
            int someVariable = 2;
            someVariable = someVariable + 1;
        }
    }
}
