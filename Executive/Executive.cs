///////////////////////////////////////////////////////////////////////
// Executive.cs - Accepts the command line arguments and passes the  //
//                  control to the respective package                //
// ver 2.1                                                           //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Dell Precision T7400, Win7, SP1                      //
// Application: Demonstration for CSE681, Project #2, Fall 2011      //
// Author:      Smruti Tatavarthy                                    //
//                                                                   //
///////////////////////////////////////////////////////////////////////
/* Package Operations
 * Accepts the command Line arguments and passes the control to the respective module
 * /S option navigates through different folders and collects .cs files from them
 * /R option display the relationships between different classes defined in the file
 * /X option displays the output in XML format
 */ 
 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis
{
    class Executive
    {
        static void Main(string[] args)
        {
           bool subDir = false;
           bool showRelation = false; 
           bool printXML = false;
           string path = "";
           if (args.Length > 0)
                path = args[0];
 
          for(int i = 0; i < args.Length ; i++)
            {
                if(args[i] == "/S")
                    subDir = true;

                if(args[i] == "/R")
                    showRelation = true;      
     
                if(args[i] == "/X")
                    printXML = true;      
            } 
            
          
            
            if (path == "")
            {
                path = "../";
            }                                
            List<string> patterns = new List<string>();
            patterns.Add("*.cs");
            string[] files = Analyzer.getFiles(path,patterns,subDir);
            List<string> listOfFiles = new List<string>(files);
            listOfFiles.Sort();
            for (int j=0; j < listOfFiles.Count; j++)
            {
                if (listOfFiles[j].Contains("TemporaryGeneratedFile") || listOfFiles[j].Contains("AssemblyInfo"))
                {
                    listOfFiles.RemoveAt(j);
                    j = j - 1;
                }
            }          
            Analyzer.doAnalysis(listOfFiles);            
            Analyzer.doRelationshipAnalysis(listOfFiles);
            Display ds = new Display();
            ds.displayPackage(showRelation);
           
            if (printXML)
            {
                ds.writeToXML(showRelation, path);
            }
        }
    }
}

 
