///////////////////////////////////////////////////////////////////////
// RulesAndActions.cs - Parser rules specific to an application      //
// ver 2.1                                                           //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Dell Precision T7400, Win7, SP1                      //
// Application: Demonstration for CSE681, Project #2, Fall 2011      //
// Author:      Jim Fawcett, CST 4-187, Syracuse University          //
//              (315) 443-3948, jfawcett@twcny.rr.com    
//Modified by:  Smruti tatavarthy
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * RulesAndActions package contains all of the Application specific
 * code required for most analysis tools.
 *
 * It defines the following Four rules which each have a
 * grammar construct detector and also a collection of IActions:
 *   - DetectNameSpace rule
 *   - DetectClass rule
 *   - DetectFunction rule
 *   - DetectScopeChange
 *   
 * Additional rules defined 
 *   - DetectInheritance rule
 *   - Detect Aggregation rule
 *   - Detect Composition rule
 *   - Detect Using rule
 *   - Detect delegates rule
 *   - Detect Anonymous Scope rule modified to calculate complexity for 
 *     functions including braceless complexity
 *   
 *   
 *   Three actions - some are specific to a parent rule:
 *   - Print
 *   - PrintFunction
 *   - PrintScope
 * 
 * The package also defines a Repository class for passing data between
 * actions and uses the services of a ScopeStack, defined in a package
 * of that name.
 * 
 * Modified by defining 2 Repositories for pass1 ans pass2
 *
 * Note:
 * This package does not have a test stub since it cannot execute
 * without requests from Parser.
 *  
 */
/* Required Files:
 *   IRuleAndAction.cs, RulesAndActions.cs, Parser.cs, ScopeStack.cs,
 *   Semi.cs, Toker.cs
 *   
 * Build command:
 *   csc /D:TEST_PARSER Parser.cs IRuleAndAction.cs RulesAndActions.cs \
 *                      ScopeStack.cs Semi.cs Toker.cs
 *   
 * Maintenance History:
 * --------------------
 * ver 2.2 : 24 Sep 2011
 * - modified Semi package to extract compile directives (statements with #)
 *   as semiExpressions
 * - strengthened and simplified DetectFunction
 * - the previous changes fixed a bug, reported by Yu-Chi Jen, resulting in
 * - failure to properly handle a couple of special cases in DetectFunction
 * - fixed bug in PopStack, reported by Weimin Huang, that resulted in
 *   overloaded functions all being reported as ending on the same line
 * - fixed bug in isSpecialToken, in the DetectFunction class, found and
 *   solved by Zuowei Yuan, by adding "using" to the special tokens list.
 * - There is a remaining bug in Toker caused by using the @ just before
 *   quotes to allow using \ as characters so they are not interpreted as
 *   escape sequences.  You will have to avoid using this construct, e.g.,
 *   use "\\xyz" instead of @"\xyz".  Too many changes and subsequent testing
 *   are required to fix this immediately.
 * ver 2.1 : 13 Sep 2011
 * - made BuildCodeAnalyzer a public class
 * ver 2.0 : 05 Sep 2011
 * - removed old stack and added scope stack
 * - added Repository class that allows actions to save and 
 *   retrieve application specific data
 * - added rules and actions specific to Project #2, Fall 2010
 * ver 1.1 : 05 Sep 11
 * - added Repository and references to ScopeStack
 * - revised actions
 * - thought about added folding rules
 * ver 1.0 : 28 Aug 2011
 * - first release
 *
 * Planned Modifications (not needed for Project #2):
 * --------------------------------------------------
 * - add folding rules:
 *   - CSemiExp returns for(int i=0; i<len; ++i) { as three semi-expressions, e.g.:
 *       for(int i=0;
 *       i<len;
 *       ++i) {
 *     The first folding rule folds these three semi-expression into one,
 *     passed to parser. 
 *   - CToker returns operator[]( as four distinct tokens, e.g.: operator, [, ], (.
 *     The second folding rule coalesces the first three into one token so we get:
 *     operator[], ( 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeAnalysis
{
    public class Elem  // holds scope information
    {
        public string type { get; set; }
        public string baseClass { get; set; }
        public string relationship { get; set; }
        public string name { get; set; }
        public int begin { get; set; }
        public int end { get; set; }       
        public int functionComplexity { get; set; }
        public int size { get; set; }
        public string fileName { get; set; }
        public string textToPrint { get; set; }
        public override string ToString()
        {
            StringBuilder temp = new StringBuilder();
            temp.Append("{");
            temp.Append(String.Format("{0,-10}", type)).Append(" : ");
            temp.Append(String.Format("{0,-10}", name)).Append(" : ");
            temp.Append(String.Format("{0,-5}", begin.ToString()));  // line of scope start
            temp.Append(String.Format("{0,-5}", end.ToString()));    // line of scope end
            temp.Append(String.Format("{0,-5}", functionComplexity.ToString()));    // line of scope end
            temp.Append("}");
            return temp.ToString();
        }
    }
    public class Pass1Repository
    {           
        static Pass1Repository instance;
        public Pass1Repository()
        {
            instance = this;
        }
        public static Pass1Repository getInstance()
        {
            return instance;
        }
        List<Elem> locations1_ = new List<Elem>();
        public List<Elem> locations1
        {
            get { return locations1_; }
        }
    }
    public class Pass2Repository
    {
        static Pass2Repository instance;
        public Pass2Repository()
        {
            instance = this;
        }
        public static Pass2Repository getInstance()
        {
            return instance;
        }
        List<Elem> locations2_ = new List<Elem>();
        public List<Elem> locations2
        {
            get { return locations2_; }
        }
    }
    public class Repository
    {
        ScopeStack<Elem> stack_ = new ScopeStack<Elem>();
        List<Elem> locations_ = new List<Elem>();
        List<Elem> relLocations_ = new List<Elem>();
        static Repository instance;
        public Repository()
        {
            instance = this;
        }
        public static Repository getInstance()
        {
            return instance;
        }
        // provides all actions access to current semiExp
        public CSsemi.CSemiExp semi
        {
            get;
            set;
        }
        // semi gets line count from toker who counts lines
        // while reading from its source
        public int lineCount  // saved by newline rule's action
        {
            get { return semi.lineCount; }
        }
        public int prevLineCount  // not used in this demo
        {
            get;
            set;
        }
        // enables recursively tracking entry and exit from scopes
        public ScopeStack<Elem> stack  // pushed and popped by scope rule's action
        {
            get { return stack_; }
        }
        // the locations table is the result returned by parser's actions
        // in this demo
        public List<Elem> locations
        {
            get { return locations_; }
        }
        public List<Elem> relLocations
        {
            get { return relLocations_; }
        }

    }
    /////////////////////////////////////////////////////////
    // pushes scope info on stack when entering new scope
    public class PushStack : AAction
    {
        Repository repo_;
        int startLine = 0;        
        public PushStack(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem = new Elem();
            elem.type = semi[0];  // expects type
            elem.name = semi[1];  // expects name
            elem.begin = repo_.semi.lineCount - 1;
            elem.end = 0;          
            repo_.stack.push(elem);
            if (elem.type == "control" || elem.name == "anonymous")
            {
                if (repo_.locations.Count > 0)
                {
                    for (int i = repo_.locations.Count - 1; i >= 0; i--)
                    {
                        if (repo_.locations[i].type == "function")
                        {
                            repo_.locations[i].functionComplexity = repo_.locations[i].functionComplexity + 1;
                            break;

                        }
                    }
                }
                return;
            }
            repo_.locations.Add(elem);
            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount - 1);
                startLine = repo_.semi.lineCount - 1;
                Console.Write("entering ");
                string indent = new string(' ', 2 * repo_.stack.count);
                Console.Write("{0}", indent);
                this.display(semi); // defined in abstract action
            }
            if (AAction.displayStack)
                repo_.stack.display();
        }
    }
    /////////////////////////////////////////////////////////
    // pops scope info from stack when leaving scope
    public class PopStack : AAction
    {
        Repository repo_;

        int endLine = 0;
        public PopStack(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem;
            try
            {
                if (repo_.stack.count < 1)
                    return;
                elem = repo_.stack.pop();
                for (int i = 0; i < repo_.locations.Count; ++i)
                {
                    Elem temp = repo_.locations[i];
                    if (elem.type == temp.type)
                    {
                        if (elem.name == temp.name)
                        {
                            if ((repo_.locations[i]).end == 0)
                            {
                                (repo_.locations[i]).end = repo_.semi.lineCount;

                                (repo_.locations[i]).size = (repo_.locations[i]).end - (repo_.locations[i]).begin;

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                string x = exp.StackTrace;
                Console.Write("popped empty stack on semiExp: ");
                Console.Write(" stack trace is  " + x);
                semi.display();
                return;
            }
            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
            local.Add(elem.type).Add(elem.name);
            if ((local[0] == "control"))
            {
                return;
            }
            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount);
                endLine = repo_.semi.lineCount;
                Console.Write("leaving  ");
                string indent = new string(' ', 2 * (repo_.stack.count + 1));
                Console.Write("{0}", indent);
                this.display(local); // defined in abstract action
            }
        }
    }
    public class RelationAnalysis : AAction
    {
        Repository repo_;
        public RelationAnalysis(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem = new Elem();
            elem.type = semi[0];  // expects type
            elem.name = semi[1];  // expects name
            Pass1Repository rep = Pass1Repository.getInstance();
            if (semi[0] == "INHERITANCE")
            {
                elem.type = "class";
                elem.name = semi[1];
                elem.baseClass = semi[2];
                for (int i = 0; i < rep.locations1.Count; i++)
                {
                    if (rep.locations1[i].name == elem.name)
                        elem.relationship = semi[0];

                }
            }
            if (semi[0] == "AGGREGATION")
            {                
                elem.name = semi[1];
                for (int i = 0; i < rep.locations1.Count; i++)
                {
                    if (rep.locations1[i].name == elem.name)
                        elem.relationship = semi[0];
                }
                for (int i = repo_.relLocations.Count; i > 0; i--)
                {
                    if (repo_.relLocations[i - 1].type == "class")
                    {
                        elem.baseClass = repo_.relLocations[i - 1].name;
                        break;
                    }
                }
            }
            if (semi[0] == "COMPOSITION")
            {
                elem.relationship = semi[0];
                for (int i = repo_.relLocations.Count; i > 0; i--)
                {
                    if (repo_.relLocations[i - 1].type == "class")
                    {
                        elem.baseClass = repo_.relLocations[i - 1].name;
                        break;
                    }
                } 
            }
            if (semi[0] == "USING")
            {
                elem.name = semi[1];
                elem.relationship = semi[0];
                for (int i = repo_.relLocations.Count; i > 0; i--)
                {
                    if (repo_.relLocations[i - 1].type == "class")
                    {
                        elem.baseClass = repo_.relLocations[i - 1].name;
                        break;
                    }
                }
            }
            elem.begin = repo_.semi.lineCount - 1;
            elem.end = 0;
            repo_.stack.push(elem);
            if (elem.type == "control" || elem.name == "anonymous")
            {

                return;
            }
            repo_.relLocations.Add(elem);
        }

    }
    ///////////////////////////////////////////////////////////
    // action to print function signatures - not used in demo
    public class PrintFunction : AAction
    {
        Repository repo_;
        public PrintFunction(Repository repo)
        {
            repo_ = repo;
        }
        public override void display(CSsemi.CSemiExp semi)
        {
            Console.Write("\n    line# {0}", repo_.semi.lineCount - 1);
            Console.Write("\n    ");
            for (int i = 0; i < semi.count; ++i)
                if (semi[i] != "\n" && !semi.isComment(semi[i]))
                    Console.Write("{0} ", semi[i]);
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            this.display(semi);
        }
    }
    /////////////////////////////////////////////////////////
    // concrete printing action, useful for debugging
    public class Print : AAction
    {
        Repository repo_;

        public Print(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Console.Write("\n  line# {0}", repo_.semi.lineCount - 1);
            this.display(semi);
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect namespace declarations
    public class DetectNamespace : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int index = semi.Contains("namespace");
            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                local.Add(semi[index]).Add(semi[index + 1]);                
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect class definitions
    public class DetectClass : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
            int indexCL = semi.Contains("class");
            int indexIF = semi.Contains("interface");
            int indexST = semi.Contains("struct");
            int indexEN = semi.Contains("enum");
            int index = Math.Max(indexCL, indexIF);
            index = Math.Max(index, indexST);
            index = Math.Max(index, indexEN);
            if (index != -1)
            {
                local.displayNewLines = false;
                local.Add(semi[index]).Add(semi[index + 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect delegate definitions
   public class DetectDelegate : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            CSsemi.CSemiExp local = new CSsemi.CSemiExp(); 
             Pass1Repository repo = Pass1Repository.getInstance();
            int indexDel = semi.Contains("delegate");           
            if (indexDel != -1)
            {
                int index = semi.FindFirst("(");
                if (index > 0)
                {
                    local.displayNewLines = false;
                    local.Add(semi[indexDel]).Add(semi[index - 1]);
                    doActions(local);
                    return true;
                }
            }
            return false;
        }
    }
   /////////////////////////////////////////////////////////
   // rule to detect class definitions
    public class DetectRelClass : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int indexCL = semi.Contains("class");
            int indexIF = semi.Contains("interface");
            int indexST = semi.Contains("struct");
            int indexEN = semi.Contains("enum");
            int index = Math.Max(indexCL, indexIF);
            index = Math.Max(index, indexST);
            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                local.Add(semi[index]).Add(semi[index + 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect Composition relations
    public class DetectComposition : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            Pass1Repository rep = Pass1Repository.getInstance();
            string compositeName = "";
            for (int i = 0; i < rep.locations1.Count; i++)
            {
                if (rep.locations1[i].type == "struct" || rep.locations1[i].type == "enum")
                {
                    compositeName = rep.locations1[i].name;
                }
            }
            int indexST = semi.Contains(compositeName);
            
            if (indexST != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                local.Add("COMPOSITION").Add(compositeName);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect Inheritance relations
    public class DetectInheritance : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
            int indexCL = semi.Contains("class");
            int indexDot = semi.Contains(".");
            if (indexCL != -1)
            {
                int indexColon = semi.Contains(":");
                if (indexColon != -1)
                {
                    if (indexDot != -1)
                    {
                        local.Add("INHERITANCE").Add(semi[indexColon - 1]).Add(semi[indexDot + 1]);
                        doActions(local);
                        return true;
                    }
                    else
                    {
                        local.Add("INHERITANCE").Add(semi[indexColon - 1]).Add(semi[indexColon + 1]);
                        doActions(local);
                        return true;
                    }
                }
            }
            local.displayNewLines = false;
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect Aggregation relations

    public class DetectAggregation : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int indexException = semi.Contains("Exception");
            int indexEquals = semi.Contains("=");
            int indexList = semi.Contains("List");
            int indexDot = semi.Contains(".");
            if (indexEquals != -1)
            {
                int index = semi.Contains("new");
                if (index != -1 && indexException == -1 && indexList == -1)
                {
                    if (indexDot != -1)
                    {
                        CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                        local.Add("AGGREGATION").Add(semi[indexDot + 1]);
                        doActions(local);
                        return true;

                    }
                    else
                    {
                        CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                        local.Add("AGGREGATION").Add(semi[index + 1]);
                        doActions(local);
                        return true;
                    }

                    
                }
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect Using relations
    public class DetectUsing : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            if (semi[semi.count - 1] != "{")
            {
                return false;
            }
            int index = semi.FindFirst("(");
            int indexDot = semi.Contains(".");
            int indexComma = semi.Contains(",");
            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
            Pass1Repository repo = Pass1Repository.getInstance();
            if (index > 0 && !isSpecialToken(semi[index - 1]))
            {
                for (int i = index; i < semi.count; i++)
                {
                    for (int j = 0; j < repo.locations1.Count; j++)
                    {                       
                            if (semi[i] == repo.locations1[j].name)
                            {
                                if (repo.locations1[j].type == "class" || repo.locations1[j].type == "delegate")
                                {
                                    local = new CSsemi.CSemiExp();
                                    local.Add("USING").Add(semi[i]);
                                    doActions(local);
                                    break;

                                }
                            }                        
                    }
                }
                return true;
            }
            local.displayNewLines = false;
            return false;
        }
        public static bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using", "switch", "string", ")" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect function definitions
    public class DetectFunction : ARule
    {
        public static bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using", "switch" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {
            if (semi[semi.count - 1] != "{")
            {
                return false;
            }
            int index = semi.FindFirst("(");
            if (index > 0 && !isSpecialToken(semi[index - 1]))
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                local.Add("function").Add(semi[index - 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // detect entering anonymous scope
    // - except namespace, class, and function scopes
    //   already handled, so put this rule after those
    public class DetectAnonymousScope : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "switch", "try", "do", "else", "elseif" };
            int index = semi.Contains("{");            
            int compIndex = 0;
            int bracelessIndex = 0;
            for (int i = 0; i < SpecialToken.Length; i++)
            {
                compIndex = semi.Contains(SpecialToken[i]);
                if (compIndex > 0 && semi[compIndex-1]!="#")
                {
                    break;

                }
            }
            if (index != -1 && compIndex != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();               
                local.displayNewLines = false;
                local.Add("control").Add("anonymous");
                doActions(local);
                return true;
            }                       
            else 
            {
                for (int i = 0; i < SpecialToken.Length; i++)
                {
                    bracelessIndex = semi.Contains(SpecialToken[i]);
                    if (bracelessIndex != -1)
                    {
                        break;
                    }
                }
                if (bracelessIndex > 0 && bracelessIndex != -1 && semi[bracelessIndex - 1] != "#")
                {
                    Repository rep = Repository.getInstance();
                    for (int i = rep.locations.Count - 1; i > 0; i--)
                    {
                        if (rep.locations[i].type == "function")
                        {
                            rep.locations[i].functionComplexity = rep.locations[i].functionComplexity + 1;
                            break;
                        }
                    }
                }           
             
           }                                  
           return false;            
        }        
    }
    /////////////////////////////////////////////////////////
    // detect leaving scope
    public class DetectLeavingScope : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int index = semi.Contains("}");
            if (index != -1)
            {
                doActions(semi);
                return true;
            }
            return false;
        }
    }
 
    public class BuildCodeAnalyzer
    {
        Repository repo = new Repository();
        public BuildCodeAnalyzer(CSsemi.CSemiExp semi)
        {
            repo.semi = semi;
        }
        public virtual Parser build()
        {
            Parser parser = new Parser();
            // decide what to show
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            // action used for namespaces, classes, and functions
            PushStack push = new PushStack(repo);

            // capture namespace info
            DetectNamespace detectNS = new DetectNamespace();
            detectNS.add(push);
            parser.add(detectNS);

            // capture class info
            DetectClass detectCl = new DetectClass();
            detectCl.add(push);
            parser.add(detectCl);

            DetectDelegate detectDel = new DetectDelegate();
            detectDel.add(push);
            parser.add(detectDel);

            // capture function info
            DetectFunction detectFN = new DetectFunction();
            detectFN.add(push);
            parser.add(detectFN);

            // handle entering anonymous scopes, e.g., if, while, etc.
            DetectAnonymousScope anon = new DetectAnonymousScope();
            anon.add(push);
            parser.add(anon);

            // handle leaving scopes
            DetectLeavingScope leave = new DetectLeavingScope();
            PopStack pop = new PopStack(repo);
            leave.add(pop);
            parser.add(leave);
            // parser configured
            return parser;
        }
        public virtual ParserPass2 build2()
        {
            ParserPass2 parser2 = new ParserPass2();

            // decide what to show
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            // action used for namespaces, classes, and functions
            RelationAnalysis relAnalysis = new RelationAnalysis(repo);

            DetectInheritance detectIn = new DetectInheritance();
            detectIn.add(relAnalysis);
            parser2.add(detectIn);

            DetectRelClass detectCl = new DetectRelClass();
            detectCl.add(relAnalysis);
            parser2.add(detectCl);

            DetectComposition detectComp = new DetectComposition();
            detectComp.add(relAnalysis);
            parser2.add(detectComp);

            DetectUsing detectUs = new DetectUsing();
            detectUs.add(relAnalysis);
            parser2.add(detectUs);

            // 09_27
            // handle relationships
            DetectAggregation rel = new DetectAggregation();
            rel.add(relAnalysis);
            parser2.add(rel);

            // parser configured
            return parser2;
        }
    }

}

