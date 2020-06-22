using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace LemulatorU.Core
{
    class VM
    {
        async void Execute(string path)
        {
            AssemblyDef asmDef = AssemblyDef.Load(path);
            await ExecuteMethod(asmDef.Modules[0].EntryPoint);
        }

        int GetInstructionIndex(uint offset, IList<Instruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Offset == offset)
                    return i - 1;
            }
            return 0;
        }

        /* Function to execute the instructions of a method */
        async Task<object> ExecuteMethod(MethodDef mdef)
        {
            /* Initialize a locals array (as dnlib is strange, we have to do this) */
            object[] Locals = new object[mdef.Body.Variables.Count];
            Stack<object> stack = new Stack<object>();

            for (int i = 0; i < mdef.Body.Instructions.Count; i++)
            {
                Instruction l = mdef.Body.Instructions[i];
                //MessageBox.Show(l.OpCode.Name + " " + (l.Operand != null ? l.Operand.ToString() : ""));
                switch (l.OpCode.Code)
                {
                    case Code.Call:
                        /* If returntype is a void, dont return anything, else push returnvalue to the stack */
                        if (l.Operand is MemberRef mref)
                        {
                            if (mref.ReturnType.FullName.Equals("System.Void"))
                                await ExecuteMember(mref, ParametersToArguments(mref, stack));
                            else
                                stack.Push(await ExecuteMember(mref, ParametersToArguments(mref, stack)));
                        }
                        else if (l.Operand is MethodDef mddef)
                        {
                            if (mddef.ReturnType.FullName.Equals("System.Void"))
                                await ExecuteMethod(mddef);
                            else
                                stack.Push(await ExecuteMethod(mddef));
                        }
                        continue;

                    /* Conditional jump operators */
                    case Code.Brtrue_S:
                        if (Convert.ToBoolean(stack.Pop()))
                            i = GetInstructionIndex(((Instruction)l.Operand).Offset, mdef.Body.Instructions);
                        continue;
                    case Code.Brfalse_S:
                        if (!Convert.ToBoolean(stack.Pop()))
                            i = GetInstructionIndex(((Instruction)l.Operand).Offset, mdef.Body.Instructions);
                        continue;

                    /* Unconditional jump operation */
                    case Code.Br_S:
                        i = GetInstructionIndex(((Instruction)l.Operand).Offset, mdef.Body.Instructions);
                        continue;

                    /* Ldc operations */
                    case Code.Ldc_I4:
                    case Code.Ldc_I4_S:
                    case Code.Ldstr:
                        stack.Push(l.Operand);
                        continue;

                    /* Arithmetic operations */
                    case Code.Add:
                        stack.Push((int)stack.Pop() + (int)stack.Pop());
                        continue;
                    case Code.Sub:
                        stack.Push((int)stack.Pop() - (int)stack.Pop());
                        continue;
                    case Code.Div:
                        stack.Push((int)stack.Pop() / (int)stack.Pop());
                        continue;
                    case Code.Mul:
                        stack.Push((int)stack.Pop() * (int)stack.Pop());
                        continue;
                    case Code.Ret:
                        if (mdef.ReturnType.FullName.Equals("System.Void"))
                            return null;
                        else
                            return stack.Pop();

                    /* Local operations */
                    case Code.Stloc_0:
                        Locals[0] = stack.Pop();
                        continue;
                    case Code.Ldloc_0:
                        stack.Push(Locals[0]);
                        continue;
                }
            }
            return null;
        }

        /* Function to run members in a virtualized environment (register functions to prevent exploits) */
        async Task<object> ExecuteMember(MemberRef mref, object[] arguments)
        {
            StringBuilder sb = new StringBuilder();

            /* Switch the class name of the member, if class name matches, check if the member is supported, else returns null */
            switch (mref.Class.Name)
            {
                case "Console": /* Class name */
                    switch (mref.Name)
                    {
                        case "WriteLine": /* Function name */
                            if (arguments.Length == 1)
                                Console.WriteLine(arguments[0]);
                            return null;

                        case "ReadLine":
                            return Console.ReadLine();
                            //return await AwaitInput(); | This is for non-console applications

                        case "set_Title":
                            if (arguments.Length == 1)
                                Console.Title = arguments[0].ToString();
                            return null;

                        default:
                            return null;
                    }
                case "String":
                    switch (mref.Name)
                    {
                        case "op_Equality":
                            bool vld = (arguments[0].ToString() == arguments[1].ToString());
                            return vld;

                        case "Concat":
                            return string.Concat(arguments.Reverse());

                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        /* Convert member's parameters into arguments (items from the stack) */
        object[] ParametersToArguments(MemberRef mref, Stack<object> stack)
        {
            object[] Arguments = new object[mref.GetParamCount()];
            for (int x = 0; x < mref.GetParamCount(); x++)
            {
                Arguments[x] = stack.Pop();
            }
            return Arguments;
        }


        /* Others */


        /* Function to get user input from the console, pauses execution until user has entered text */
        //bool IsSent = false;
        //async Task<string> AwaitInput()
        //{
        //    while (!IsSent)
        //    {
        //        await Task.Delay(100);
        //    }
        //    IsSent = false;
        //    string ret = textBox3.Text;
        //    ConsOut.Items.RemoveAt(ConsOut.Items.Count - 1);
        //    WriteCons("> " + ret);
        //    textBox3.Clear();
        //    return ret;
        //}
    }
}
