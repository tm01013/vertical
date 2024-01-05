using System.Data;
using System.Linq.Expressions;
using org.matheval;
using System.IO;
using System;
public class Program
{

    public static void Main(string[] args)
    {
        if (args.Length < 1) Err("Usage: \n  vertical <script path>\n");
        string filePath = args[0];
        //string filePath = "/Users/TM/vscode/Vertical/Vertical/test.vs"; //for testing

        string program = "";

        //Pass the file path and file name to the StreamReader constructor
        StreamReader sr = new StreamReader(filePath);
        //Read the first line of text
        program = sr.ReadToEnd();

        //close the file
        sr.Close();

        /*Console.WriteLine("--------");
        Console.Write(ToHorizontal(program));
        Console.WriteLine("--------");*/

        List<Variable> variables = new List<Variable>();
        List<Column> columns = new List<Column>();

        //  Prepearing...
        foreach (string column in ToHorizontal(program).Split('\n').ToList())
        {
            Column colToAdd = new Column(
                ToHorizontal(program).Split('\n').ToList().IndexOf(column),
                column
            );
            columns.Add(colToAdd);
        }

        int currentCol = 0;
        Staus currentStatus = Staus.None;

        int startChar = 0;

        List<IfStatement>[] ifStatements = TokenizeIfStatements();
        int currentIfqMarkPos = 0;
        List<int> currentIfqMarkIds = new List<int>();


    mainFlow:

        string currentVariableName = "";

        string currentStringInProcess = "";
        bool currentStringIsStartedProcessing = false;

        string currentExpression = "";
        bool currentIntIsStartedProcessing = false;

        string currentIfStatement = "";

        string laberIdInBuilding = "";

        char[] col = columns[currentCol].value.ToCharArray();

        for (int i = startChar; i < col.Length; i++)
        {
            switch (currentStatus)
            {
                case Staus.None:
                    if (col[i] == 'Q') Environment.Exit(0);
                    else if (col[i] == 'P') currentStatus = Staus.PrintStatement;
                    else if (col[i] == 'W') currentStatus = Staus.WriteStatement;
                    else if (col[i] == '§') currentStatus = Staus.AddingExecutionPoint;
                    else if (col[i] == 'G')
                    {
                        List<char> charsInCol = columns[currentCol].value.ToCharArray().ToList();

                        if (charsInCol.Count - 1 < i + 2) Err("Fatal Error at " + currentCol + "," + i);

                        char nextChar = charsInCol[i + 2];
                        if (nextChar == '#')
                        {
                            currentVariableName = charsInCol[i + 1].ToString();
                            currentStatus = Staus.MakingGlobalInt;
                            currentExpression = "";
                            currentIntIsStartedProcessing = false;
                        }
                        else if (nextChar == '"')
                        {
                            currentVariableName = charsInCol[i + 1].ToString();
                            currentStatus = Staus.MakingGlobalString;
                            currentStringIsStartedProcessing = false;
                            currentStringInProcess = "";
                        }
                        else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    }
                    else if (col[i] == '@')
                    {
                        currentStatus = Staus.Goto;
                        laberIdInBuilding = "";
                    }
                    else if (col[i] == ' ') currentStatus = Staus.None;
                    else if (col[i] == '?')
                    {
                        currentStatus = Staus.IfStatement;
                        currentIfqMarkPos = i;
                        currentIfStatement = "";
                    }
                    else if (col[i].ToString().ToUpper() != col[i].ToString())
                    {
                        currentStatus = Staus.MakingVariable;
                        List<char> charsInCol = columns[currentCol].value.ToCharArray().ToList();

                        if (charsInCol.Count - 1 < charsInCol.IndexOf(col[i]) + 1) Err("Fatal Error at " + currentCol + "," + i);

                        char nextChar = charsInCol[charsInCol.IndexOf(col[i]) + 1];
                        if (nextChar == '#')
                        {
                            currentStatus = Staus.MakingInt;
                            currentExpression = "";
                            currentIntIsStartedProcessing = false;
                        }
                        else if (nextChar == '"')
                        {
                            currentStatus = Staus.MakeingString;
                            currentStringIsStartedProcessing = false;
                            currentStringInProcess = "";
                        }
                        else Error(ErrType.InvalidCharacter, i, col[i].ToString());

                        currentVariableName = col[i].ToString();
                    }
                    else if (col[i] == '\\' || col[i] == '_')
                    {
                        if (currentIfqMarkIds.Count > 0)
                        {
                            IfStatement lastIf = ifStatements[currentCol].Where(s => s.questionMarkPos == currentIfqMarkIds[currentIfqMarkIds.Count - 1]).ToList()[0];

                            if (i == lastIf.falseMarkPos && col[i] == '\\')
                            {
                                currentIfqMarkIds.RemoveAt(currentIfqMarkIds.Count - 1);

                                int characterPos = lastIf.endMarkPos + 1;
                                int colId = currentCol;

                                if (col.Count() - 1 <= characterPos) Err("Fatal Error at " + currentCol + "," + i);
                                if (characterPos < 0) Err("Fatal Error at " + currentCol + "," + i);

                                currentStatus = Staus.None;
                                startChar = characterPos;

                                goto mainFlow;
                            }
                            else if (i == lastIf.endMarkPos && col[i] == '_')
                            {
                                currentIfqMarkIds.RemoveAt(currentIfqMarkIds.Count - 1);

                                currentStatus = Staus.None;
                            }
                            else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                        }
                        else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    }
                    else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    break;

                case Staus.MakeingString:
                    if (col[i] == '\'' && currentStringIsStartedProcessing)
                    {
                        if (variables.Where(v => v.name == currentVariableName && v.isGlobal).Any()) Error(ErrType.VariableAlreadyExsitWithOtherScope, i, currentVariableName);
                        if (variables.Where(v => v.name == currentVariableName && v.colId == currentCol && !v.isGlobal && v.variableType == VariableTypes.Int).Any()) Error(ErrType.VariableAlreadyExsitWithOtherType, i, currentVariableName);

                        if (variables.Where(v => v.name == currentVariableName && v.colId == currentCol && !v.isGlobal && v.variableType == VariableTypes.String).Any())
                        {
                            variables[variables.IndexOf(variables.Where(v => v.name == currentVariableName && v.colId == currentCol && !v.isGlobal && v.variableType == VariableTypes.String).ToList()[0])] = new Variable
                            {
                                isGlobal = false,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.String,
                                value = currentStringInProcess
                            };

                            currentVariableName = "";
                            currentStringInProcess = "";
                        }
                        else if (!variables.Where(v => v.name == currentVariableName && v.variableType == VariableTypes.String).Any())
                        {
                            variables.Add(new Variable
                            {
                                isGlobal = false,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.String,
                                value = currentStringInProcess
                            });

                            currentVariableName = "";
                            currentStringInProcess = "";
                        }
                        else Error(ErrType.Fatal, i, "");

                        currentStatus = Staus.None;
                        currentStringIsStartedProcessing = false;
                    }
                    else if (col[i] == '"' && !currentStringIsStartedProcessing) currentStringIsStartedProcessing = true;
                    else
                    {
                        currentStringInProcess += col[i].ToString();
                    }

                    break;
                case Staus.MakingGlobalString:
                    if (col[i] == '\'' && currentStringIsStartedProcessing)
                    {
                        if (variables.Where(v => v.name == currentVariableName && !v.isGlobal).Any()) Error(ErrType.VariableAlreadyExsitWithOtherScope, i, currentVariableName);

                        if (variables.Where(v => v.name == currentVariableName && v.isGlobal && v.variableType == VariableTypes.Int).Any()) Error(ErrType.VariableAlreadyExsitWithOtherType, i, currentVariableName);

                        if (variables.Where(v => v.name == currentVariableName && v.isGlobal && v.variableType == VariableTypes.String).Any())
                        {
                            variables[variables.IndexOf(variables.Where(v => v.name == currentVariableName && v.isGlobal && v.variableType == VariableTypes.String).ToList()[0])] = new Variable
                            {
                                isGlobal = true,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.String,
                                value = currentStringInProcess
                            };

                            currentVariableName = "";
                            currentStringInProcess = "";
                        }
                        else
                        {
                            variables.Add(new Variable
                            {
                                isGlobal = true,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.String,
                                value = currentStringInProcess
                            });

                            currentVariableName = "";
                            currentStringInProcess = "";
                        }

                        currentStatus = Staus.None;
                        currentStringIsStartedProcessing = false;
                    }
                    else if (col[i] == '"' && !currentStringIsStartedProcessing) currentStringIsStartedProcessing = true;
                    else if (col[i].ToString() == currentVariableName) { }
                    else
                    {
                        currentStringInProcess += col[i].ToString();
                    }
                    break;

                case Staus.MakingInt:
                    if (int.TryParse(col[i].ToString(), out _))
                    {
                        currentExpression += col[i].ToString();
                    }
                    else if (col[i] == ' ') { }
                    else if (col[i] == '#' && !currentIntIsStartedProcessing) { currentIntIsStartedProcessing = true; }
                    else if (col[i] == '-') currentExpression += col[i].ToString();
                    else if (col[i] == '+') currentExpression += col[i].ToString();
                    else if (col[i] == '*') currentExpression += col[i].ToString();
                    else if (variables.Where(v => v.name == col[i].ToString() && v.variableType == VariableTypes.Int).Any())
                    {
                        currentExpression += col[i].ToString();
                    }
                    else if (col[i] == '#' && currentIntIsStartedProcessing)
                    {
                        int result = SolveMathExpression(currentExpression);

                        if (variables.Where(v => v.name == currentVariableName && v.isGlobal).Any()) Error(ErrType.VariableAlreadyExsitWithOtherScope, i, currentVariableName);
                        if (variables.Where(v => v.name == currentVariableName && v.colId == currentCol && !v.isGlobal && v.variableType == VariableTypes.String).Any()) Error(ErrType.VariableAlreadyExsitWithOtherType, i, currentVariableName);

                        if (variables.Where(v => v.name == currentVariableName && v.colId == currentCol && !v.isGlobal && v.variableType == VariableTypes.Int).Any())
                        {
                            variables[variables.IndexOf(variables.Where(v => v.name == currentVariableName && v.colId == currentCol && !v.isGlobal).ToList()[0])] = new Variable
                            {
                                isGlobal = false,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.Int,
                                value = result.ToString()
                            };

                            currentVariableName = "";
                            currentExpression = "";
                            currentStatus = Staus.None;
                        }
                        else
                        {
                            variables.Add(new Variable
                            {
                                isGlobal = false,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.Int,
                                value = result.ToString()
                            });

                            currentVariableName = "";
                            currentExpression = "";
                            currentStatus = Staus.None;
                        }
                    }

                    break;
                case Staus.MakingGlobalInt:
                    if (int.TryParse(col[i].ToString(), out _))
                    {
                        currentExpression += col[i].ToString();
                    }
                    else if (col[i] == ' ') { }
                    else if (col[i] == '#' && !currentIntIsStartedProcessing) { currentIntIsStartedProcessing = true; }
                    else if (col[i].ToString() == currentVariableName && !currentIntIsStartedProcessing) { }
                    else if (col[i] == '-') currentExpression += col[i].ToString();
                    else if (col[i] == '+') currentExpression += col[i].ToString();
                    else if (col[i] == '*') currentExpression += col[i].ToString();
                    else if (variables.Where(v => v.name == col[i].ToString() && v.variableType == VariableTypes.Int).Any())
                    {
                        currentExpression += col[i].ToString();
                    }
                    else if (col[i] == '#' && currentIntIsStartedProcessing)
                    {
                        int result = SolveMathExpression(currentExpression);

                        if (variables.Where(v => v.name == currentVariableName && !v.isGlobal).Any()) Error(ErrType.VariableAlreadyExsitWithOtherScope, i, currentVariableName);
                        if (variables.Where(v => v.name == currentVariableName && v.isGlobal && v.variableType == VariableTypes.String).Any()) Error(ErrType.VariableAlreadyExsitWithOtherType, i, currentVariableName);

                        if (variables.Where(v => v.name == currentVariableName && v.isGlobal && v.variableType == VariableTypes.Int).Any())
                        {
                            variables[variables.IndexOf(variables.Where(v => v.name == currentVariableName && v.isGlobal).ToList()[0])] = new Variable
                            {
                                isGlobal = true,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.Int,
                                value = result.ToString()
                            };

                            currentVariableName = "";
                            currentExpression = "";
                            currentStatus = Staus.None;
                        }
                        else
                        {
                            variables.Add(new Variable
                            {
                                isGlobal = true,
                                colId = currentCol,
                                name = currentVariableName,
                                variableType = VariableTypes.Int,
                                value = result.ToString()
                            });

                            currentVariableName = "";
                            currentExpression = "";
                            currentStatus = Staus.None;
                        }
                    }
                    break;

                case Staus.IfStatement:
                    if (col[i] != '/') currentIfStatement += col[i].ToString();
                    else if (col[i] == '/')
                    {
                        bool ifResult;
                        ConditionalSolver(currentIfStatement, out ifResult);
                        if (ifResult)
                        {
                            currentStatus = Staus.None;

                            currentIfqMarkIds.Add(currentIfqMarkPos);
                        }
                        else if (!ifResult)
                        {
                            int characterPos = 0;
                            int colId = currentCol;
                            if (ifStatements[currentCol].Where(s => s.questionMarkPos == currentIfqMarkPos).Any())
                            {
                                characterPos = ifStatements[currentCol].Where(s => s.questionMarkPos == currentIfqMarkPos).ToList()[0].falseMarkPos;
                            }
                            else Error(ErrType.LabelNotFound, i, "/");


                            if (!columns.Where(c => c.colId == colId).Any()) Error(ErrType.Fatal, i, "");
                            if (!columns.Where(c => c.value.Length > characterPos).Any()) Error(ErrType.Fatal, i, "");
                            if (characterPos < 0) Error(ErrType.Fatal, i, "");
                            currentStatus = Staus.None;
                            startChar = characterPos + 1;

                            currentIfqMarkIds.Add(currentIfqMarkPos);

                            goto mainFlow;
                        }
                    }
                    break;
                case Staus.PrintStatement:
                    if (col[i].ToString().ToUpper() != col[i].ToString())
                    {
                        if (variables.Where(v => v.name == col[i].ToString() && (v.colId == currentCol || v.isGlobal)).Any())
                        {
                            Console.WriteLine(variables.Where(v => v.name == col[i].ToString() && (v.colId == currentCol || v.isGlobal)).ToList()[0].value);
                        }
                        else Error(ErrType.VariableNotFound, i, col[i].ToString());
                    }
                    else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    currentStatus = Staus.None;
                    break;
                case Staus.WriteStatement:
                    if (col[i].ToString().ToUpper() != col[i].ToString())
                    {
                        if (variables.Where(v => v.name == col[i].ToString() && (v.colId == currentCol || v.isGlobal)).Any())
                        {
                            Console.Write(variables.Where(v => v.name == col[i].ToString() && (v.colId == currentCol || v.isGlobal)).ToList()[0].value);
                        }
                        else Error(ErrType.VariableNotFound, i, col[i].ToString());
                    }
                    else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    currentStatus = Staus.None;
                    break;
                case Staus.Goto:
                    if (Char.IsDigit(col[i]))
                    {
                        laberIdInBuilding += col[i].ToString();
                    }
                    else if (laberIdInBuilding != "")
                    {
                        bool isFound = false;
                        int character = 0;
                        int columnId = 0;

                        string label = "§" + laberIdInBuilding;

                        foreach (Column column in columns)
                        {
                            if (column.value.Contains(label))
                            {
                                isFound = true;
                                character = column.value.IndexOf(label);
                                columnId = column.colId;
                            }
                        }
                        if (!isFound) Error(ErrType.LabelNotFound, i, label);


                        if (!columns.Where(c => c.colId == columnId).Any()) Error(ErrType.Fatal, i, "");
                        if (!columns.Where(c => c.value.Length > character).Any()) Error(ErrType.Fatal, i, "");
                        if (character < 0) Error(ErrType.Fatal, i, "");

                        currentStatus = Staus.None;
                        startChar = character;
                        currentCol = columnId;

                        currentIfqMarkIds = new List<int>();

                        goto mainFlow;
                    }
                    else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    break;
                case Staus.AddingExecutionPoint:
                    //ignore
                    if (Char.IsDigit(col[i]))
                    {
                        laberIdInBuilding += col[i].ToString();
                    }
                    else if (laberIdInBuilding != "")
                    {
                        laberIdInBuilding = "";
                        currentStatus = Staus.None;
                    }
                    else Error(ErrType.InvalidCharacter, i, col[i].ToString());
                    break;
            }
        }

        List<IfStatement>[] TokenizeIfStatements()
        {
            List<IfStatement>[] result = new List<IfStatement>[columns.Count];
            // ----------------initialize-----------------
            for (int i = 0; i < columns.Count; i++)
            {
                result[i] = new List<IfStatement>();
            }

            for (int colId = 0; colId < columns.Count; colId++)
            {
                List<int> qMarkStack = new List<int>();
                List<IfStatement> statementsInBuilding = new List<IfStatement>();

                char[] col = columns[colId].value.ToCharArray();

                for (int i = 0; i < col.Length; i++)
                {
                    if (columns[colId].value[i] == '?')
                    {
                        bool isInString = true;

                        // is in a string?
                        int b = i;
                        while ((columns[colId].value[b] != '"' || columns[colId].value[b] != '\'') && b > 0)
                        {
                            b--;
                        }
                        if (b == 0)
                        {
                            isInString = false;
                            goto isNotString;
                        }
                        /*
                        a"?a' a"bbbb'
                        */
                        if (columns[colId].value[b] != '"')
                        {
                            if (columns[colId].value[b] == '\'')
                            {
                                isInString = false;
                                goto isNotString;
                            }
                        }

                    isNotString:
                        if (!isInString)
                        {
                            qMarkStack.Add(i);
                        }
                    }
                    else if (columns[colId].value[i] == '/')
                    {
                        bool isInString = true;

                        // is in a string?
                        int b = i;
                        while ((columns[colId].value[b] != '"' || columns[colId].value[b] != '\'') && b > 0)
                        {
                            b--;
                        }
                        if (b == 0)
                        {
                            isInString = false;
                            goto isNotString;
                        }
                        /*
                        a"?a' a"bbbb'
                        */
                        if (columns[colId].value[b] != '"')
                        {
                            if (columns[colId].value[b] == '\'')
                            {
                                isInString = false;
                                goto isNotString;
                            }
                        }

                    isNotString:
                        if (!isInString)
                        {
                            statementsInBuilding.Add(new IfStatement(colId, qMarkStack[qMarkStack.Count - 1], i, 0, 0));
                        }
                    }
                    else if (columns[colId].value[i] == '\\')
                    {
                        bool isInString = true;

                        // is in a string?
                        int b = i;
                        while ((columns[colId].value[b] != '"' || columns[colId].value[b] != '\'') && b > 0)
                        {
                            b--;
                        }
                        if (b == 0)
                        {
                            isInString = false;
                            goto isNotString;
                        }
                        /*
                        a"?a' a"bbbb'
                        */
                        if (columns[colId].value[b] != '"')
                        {
                            if (columns[colId].value[b] == '\'')
                            {
                                isInString = false;
                                goto isNotString;
                            }
                        }

                    isNotString:
                        if (!isInString)
                        {
                            IfStatement statement = statementsInBuilding.Where(s => s.colId == colId && s.questionMarkPos == qMarkStack[qMarkStack.Count - 1]).ToList()[0];
                            IfStatement newIf = new IfStatement(statement.colId, statement.questionMarkPos, statement.trueMarkPos, i, 0);
                            statementsInBuilding.Remove(statement);
                            statementsInBuilding.Add(newIf);
                        }
                    }
                    else if (columns[colId].value[i] == '_')
                    {
                        bool isInString = true;

                        // is in a string?
                        int b = i;
                        while ((columns[colId].value[b] != '"' || columns[colId].value[b] != '\'') && b > 0)
                        {
                            b--;
                        }
                        if (b == 0)
                        {
                            isInString = false;
                            goto isNotString;
                        }
                        /*
                        a"?a' a"bbbb'
                        */
                        if (columns[colId].value[b] != '"')
                        {
                            if (columns[colId].value[b] == '\'')
                            {
                                isInString = false;
                                goto isNotString;
                            }
                        }

                    isNotString:
                        if (!isInString)
                        {
                            IfStatement statement = statementsInBuilding.Where(s => s.colId == colId && s.questionMarkPos == qMarkStack[qMarkStack.Count - 1]).ToList()[0];
                            IfStatement newIf = new IfStatement(statement.colId, statement.questionMarkPos, statement.trueMarkPos, statement.falseMarkPos, i);
                            statementsInBuilding.Remove(statement);
                            statementsInBuilding.Add(newIf);
                            qMarkStack.RemoveAt(qMarkStack.Count - 1);
                        }
                    }
                }

                if (qMarkStack.Count > 0) Err("If statement has no matching end tag!");
                result[colId] = statementsInBuilding;
            }

            return result;
        }

        void ConditionalSolver(string statement, out bool result)
        {
            /*
                | < > !
            */
            statement.Replace(" ", "");
            result = false;

            if (statement.Contains(">") && !statement.Contains("<") && !statement.Contains("|") && !statement.Contains("!"))
            {
                string[] sites = statement.Split('>');
                if (sites.Count() != 2) Err("Invalid expression format!");

                VariableTypes variableType = VariableTypes.None;

                foreach (char c in sites[0])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }
                foreach (char c in sites[1])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }

                if (variableType == VariableTypes.Int)
                {
                    int leftSide = SolveMathExpression(sites[0]);
                    int rightSide = SolveMathExpression(sites[1]);

                    if (leftSide > rightSide) result = true;
                    else result = false;
                }
                if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, ">");
            }
            else if (!statement.Contains(">") && statement.Contains("<") && !statement.Contains("|") && !statement.Contains("!"))
            {
                string[] sites = statement.Split('<');
                if (sites.Count() != 2) Err("Invalid expression format!");

                VariableTypes variableType = VariableTypes.None;

                foreach (char c in sites[0])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }
                foreach (char c in sites[1])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }

                if (variableType == VariableTypes.Int)
                {
                    int leftSide = SolveMathExpression(sites[0]);
                    int rightSide = SolveMathExpression(sites[1]);

                    if (leftSide < rightSide) result = true;
                    else result = false;
                }
                if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, "<");
            }
            else if (!statement.Contains(">") && !statement.Contains("<") && statement.Contains("|") && !statement.Contains("!"))
            {
                string[] sites = statement.Split('|');
                if (sites.Count() != 2) Err("Invalid expression format!");

                VariableTypes variableType = VariableTypes.None;

                foreach (char c in sites[0])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }
                foreach (char c in sites[1])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }

                if (variableType == VariableTypes.Int)
                {
                    int leftSide = SolveMathExpression(sites[0]);
                    int rightSide = SolveMathExpression(sites[1]);

                    if (leftSide == rightSide) result = true;
                    else result = false;
                }
                if (variableType == VariableTypes.String)
                {
                    string leftSide = "";
                    foreach (char c in sites[0])
                    {
                        if (c == ' ') { }
                        else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                        {
                            leftSide = variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).ToList()[0].value;
                            break;
                        }
                        else Error(ErrType.InvalidCharacter, -1, c.ToString());
                    }
                    string rightSide = "";
                    foreach (char c in sites[1])
                    {
                        if (c == ' ') { }
                        else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                        {
                            rightSide = variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).ToList()[0].value;
                            break;
                        }
                        else Error(ErrType.InvalidCharacter, -1, c.ToString());
                    }

                    if (rightSide == leftSide) result = true;
                    else result = false;
                }
            }
            else if (!statement.Contains(">") && !statement.Contains("<") && !statement.Contains("|") && statement.Contains("!"))
            {
                string[] sites = statement.Split('!');
                if (sites.Count() != 2) Err("Invalid expression format!");

                VariableTypes variableType = VariableTypes.None;

                foreach (char c in sites[0])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }
                foreach (char c in sites[1])
                {
                    if (c == '+' || c == '-' || c == '*')
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.Int).Any())
                    {
                        if (variableType == VariableTypes.String) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.Int;
                    }
                    else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                    {
                        if (variableType == VariableTypes.Int) Error(ErrType.BadOperator, 0, c.ToString());
                        variableType = VariableTypes.String;
                    }
                }

                if (variableType == VariableTypes.Int)
                {
                    int leftSide = SolveMathExpression(sites[0]);
                    int rightSide = SolveMathExpression(sites[1]);

                    if (leftSide != rightSide) result = true;
                    else result = false;
                }
                if (variableType == VariableTypes.String)
                {
                    string leftSide = "";
                    foreach (char c in sites[0])
                    {
                        if (c == ' ') { }
                        else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                        {
                            leftSide = variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).ToList()[0].value;
                            break;
                        }
                        else Error(ErrType.InvalidCharacter, -1, c.ToString());
                    }
                    string rightSide = "";
                    foreach (char c in sites[1])
                    {
                        if (c == ' ') { }
                        else if (variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).Any())
                        {
                            rightSide = variables.Where(v => v.name == c.ToString() && (v.colId == currentCol || v.isGlobal) && v.variableType == VariableTypes.String).ToList()[0].value;
                            break;
                        }
                        else Error(ErrType.InvalidCharacter, -1, c.ToString());
                    }

                    if (rightSide != leftSide) result = true;
                    else result = false;
                }
            }

            else
            {
                Err("Invalid expression format!");
            }
        }

        int SolveMathExpression(string expression)
        {
            int result = 0;

            for (int i = 0; i < expression.Length; i++)
            {
                if (expression[i].ToString().ToLower() != expression[i].ToString().ToUpper())
                {
                    if (variables.Where(v => v.variableType == VariableTypes.Int && v.name == expression[i].ToString() && ((v.colId == currentCol && !v.isGlobal) || v.isGlobal)).Any())
                    {
                        expression = expression.Replace(expression[i].ToString(), variables.Where(v => v.variableType == VariableTypes.Int && v.name == expression[i].ToString() && ((v.colId == currentCol && !v.isGlobal) || v.isGlobal)).ToList()[0].value);
                    }
                    else Error(ErrType.VariableNotFound, -1, expression[i].ToString());
                }
            }

            expression = expression.Replace(" ", "");
            expression = expression.Replace("--", "+");
            expression = expression.Replace("++", "+");
            expression = expression.Replace("-+", "-");
            expression = expression.Replace("+-", "-");
            expression = expression.Replace("*+", "*");

            result = Decimal.ToInt32((decimal)new org.matheval.Expression(expression).Eval());

            return result;
        }

        string ToHorizontal(string input)
        {
            string output = "";
            if (input != String.Empty)
            {
                string[] colSring = input.Split('\n');
                int cols = 0;
                for (int i = 0; i < colSring.Length; i++)
                {
                    // + Cleanup
                    colSring[i] = String.Join("", colSring[i].Split('\r', '\t'));
                    if (colSring[i].Length > cols) cols = colSring[i].Length;
                }
                int maxRows = colSring.Length;
                Column[] columns = new Column[cols];

                for (int i = 0; i < cols; i++)
                {
                    columns[i] = new Column(i, "");
                }

                foreach (string row in colSring)
                {
                    string str = row;
                    while (str.Length < cols)
                    {
                        str += ' ';
                    }

                    if (str != string.Empty)
                    {
                        for (int i = 0; i < str.Length; i++)
                        {
                            columns[i] = new Column(i, columns[i].value + str[i]);
                        }
                    }
                }

                foreach (var column in columns)
                {
                    output = output + column.value + '\n';
                }
            }
            return output;
        }

        void Error(ErrType type, int charId, string argument)
        {
            switch (type)
            {
                case ErrType.Fatal:
                    Console.WriteLine("Unknown error at column " + currentCol + " at character " + charId);
                    break;
                case ErrType.InvalidCharacter:
                    Console.WriteLine("Invalid character: " + argument + " at column " + currentCol + "," + charId);
                    break;
                case ErrType.BadOperator:
                    Console.WriteLine("Bad operator: " + argument + " at column " + currentCol);
                    break;
                case ErrType.VariableNotFound:
                    Console.WriteLine("Variable not found: " + argument + " at column " + currentCol + "," + charId);
                    break;
                case ErrType.VariableAlreadyExsitWithOtherType:
                    Console.WriteLine("Variable already exsit with another type: " + argument + " at column " + currentCol + "," + charId);
                    break;
                case ErrType.VariableAlreadyExsitWithOtherScope:
                    Console.WriteLine("Variable already exsit in a conflicting scpoe: " + argument + " at column " + currentCol + "," + charId);
                    break;
                case ErrType.LabelNotFound:
                    Console.WriteLine("Label not found " + argument + " at column " + currentCol + "," + charId);
                    break;
            }
            Environment.Exit(1);
        }

        void Err(string customMessage)
        {
            Console.WriteLine(customMessage);
            Environment.Exit(1);
        }
    }
    public class Variable
    {
        public bool isGlobal;
        public int colId;
        public required string name;
        public VariableTypes variableType;
        public required string value;
    }

    public class Column
    {
        public int colId;
        public string value;

        public Column(int id, string value)
        {
            this.colId = id;
            this.value = value;
        }
    }

    public class IfStatement
    {
        public int colId;
        public int questionMarkPos;
        public int trueMarkPos;
        public int falseMarkPos;
        public int endMarkPos;

        public IfStatement(int col, int qPos, int tPos, int fPos, int ePos)
        {
            colId = col;
            questionMarkPos = qPos;
            trueMarkPos = tPos;
            falseMarkPos = fPos;
            endMarkPos = ePos;
        }
    }

    public enum ErrType
    {
        Fatal,
        InvalidCharacter,
        BadOperator,
        VariableNotFound,
        VariableAlreadyExsitWithOtherType,
        VariableAlreadyExsitWithOtherScope,
        LabelNotFound
    }

    public enum ExpressionStatus
    {
        None,
        Substracting,
        Adding,
        Multiplying
    }

    public enum Staus
    {
        None,

        MakingVariable,

        MakeingString,
        MakingGlobalString,

        MakingInt,
        MakingGlobalInt,

        IfStatement,

        PrintStatement,
        WriteStatement,
        Goto,
        AddingExecutionPoint

    }

    public enum VariableTypes
    {
        None,
        Int,
        String
    }

}