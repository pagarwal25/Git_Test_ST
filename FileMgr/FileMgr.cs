///////////////////////////////////////////////////////////////////////
// File Manager.cs -Manages Files to be parsed                       //
// ver 2.1                                                           //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Dell Precision T7400, Win7, SP1                      //
// Application: Demonstration for CSE681, Project #2, Fall 2011      //
// Author:      Jim Fawcett, CST 4-187, Syracuse University          //
//              (315) 443-3948, jfawcett@twcny.rr.com    
//
///////////////////////////////////////////////////////////////////////
/* Package Operations
 * Creates a list of files and patterns
 * Finds the files files from the subdirectories and adds them to the list
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CodeAnalysis
{
    public class FileMgr
    {
        private List<string> files = new List<string>();
        private List<string> patterns = new List<string>();       
        public void findFiles(string path, bool subDir)
        {
            if (patterns.Count == 0)
                addPattern("*.*");
            foreach(string pattern in patterns)
            {
                string[] newFiles = Directory.GetFiles(path,pattern);
                for (int i = 0; i < newFiles.Length; ++i)                
                        newFiles[i] = Path.GetFullPath(newFiles[i]);
                files.AddRange(newFiles);
            }          
            if(subDir)
            {
                string[] dirs= Directory.GetDirectories(path);
                foreach (string dir in dirs)
                    findFiles(dir, subDir);
            }
        }
        public void addPattern(string pattern)
        {
            patterns.Add(pattern);
        }
        public List<string> getFiles()
        {
            return files;
        }

#if(TEST_FILEMGR)
        static void Main(string[] args)
        {
            Console.Write("\n Testing FileMgr Class");
            Console.Write("\n ======================\n");
            FileMgr fm = new FileMgr();
            fm.addPattern("*.cs");
            fm.findFiles("../../",false);
            List<string> files= fm.getFiles();
            foreach (string file in files)
            {
                Console.Write("\n {0}", file);
            }
            Console.Write("\n\n");
            
        }
#endif
    }
}
