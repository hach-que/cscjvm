using System;
using System.IO;
using ICSharpCode.NRefactory.TypeSystem;

namespace cscjvm
{
    public class JavaBytecodeWriter
    {
        private StreamWriter m_Writer;
        private int m_CurrentStack = 0;
        private int p_MaxStack = 0;

        public int MaxStack
        {
            get { return this.p_MaxStack; }
        }

        public JavaBytecodeWriter(StreamWriter writer)
        {
            this.m_Writer = writer;
        }

        private void Push()
        {
            this.m_CurrentStack++;
            if (this.m_CurrentStack > this.p_MaxStack)
                this.p_MaxStack = this.m_CurrentStack;
        }

        private void Pop()
        {
            this.m_CurrentStack--;
        }

        #region Load Constants

        public void LoadConstant(string value)
        {
            this.m_Writer.WriteLine("ldc \"" + value.Replace("\\","\\\\").Replace("\"","\\\"") + "\"");
            this.Push();
        }

        public void LoadConstant(int value)
        {
            this.m_Writer.WriteLine("ldc " + value);
            this.Push();
        }

        public void LoadConstant(float value)
        {
            this.m_Writer.WriteLine("ldc " + value);
            this.Push();
        }

        public void LoadConstant(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            this.m_Writer.WriteLine("ldc2_w " + BitConverter.ToInt32(new[] { bytes[0] }, 0) + ", " + new[] { bytes[1] }, 0);
            this.Push();
        }

        public void LoadConstant(double value)
        {
            var bytes = BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(value));
            this.m_Writer.WriteLine("ldc2_w " + BitConverter.ToInt32(new[] { bytes[0] }, 0) + ", " + new[] { bytes[1] }, 0);
            this.Push();
        }

        #endregion

        #region Member Operations
        
        public void MemberGetStatic(IMember member)
        {
            this.m_Writer.WriteLine("getstatic " + member.FullName.ToJavaNamespace() + 
                                    " " + JavaSignature.CreateTypeSignature(member.ReturnType));
            this.Push();
        }

        #endregion

        #region Function Operations
        
        public void FunctionInvoke(IMethod method)
        {
            this.m_Writer.Write(JavaInvoke.DetermineInvocationMethod(method));
            this.m_Writer.Write(' ');
            this.m_Writer.WriteLine(JavaSignature.CreateMethodSignature(method));
            if (JavaInvoke.DetermineInvocationMethod(method) != "getstatic")
                this.Pop();
            for (var i = 0; i < method.Parameters.Count; i++)
                this.Pop();
        }

        #endregion

        #region Reference Operations

        public void ReferenceStore(int local)
        {
            this.m_Writer.WriteLine("astore " + local);
            this.Pop();
        }

        public void ReferenceLoad(int local)
        {
            this.m_Writer.WriteLine("aload " + local);
            this.Push();
        }

        #endregion

        #region Integer Operations

        public void IntAdd()
        {
            this.m_Writer.WriteLine("iadd");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void IntSubtract()
        {
            this.m_Writer.WriteLine("isub");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void IntMultiply()
        {
            this.m_Writer.WriteLine("imul");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void IntDivide()
        {
            this.m_Writer.WriteLine("idiv");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void IntModulo()
        {
            this.m_Writer.WriteLine("irem");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void IntNegate()
        {
            this.m_Writer.WriteLine("ineg");
        }

        public void IntReturn()
        {
            this.m_Writer.WriteLine("ireturn");
        }

        public void IntStore(int local)
        {
            this.m_Writer.WriteLine("istore " + local);
            this.Pop();
        }

        public void IntLoad(int local)
        {
            this.m_Writer.WriteLine("iload " + local);
            this.Push();
        }

        #endregion
        
        #region Long Operations

        public void LongAdd()
        {
            this.m_Writer.WriteLine("ladd");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void LongStore(int local)
        {
            this.m_Writer.WriteLine("lstore " + local);
            this.Pop();
        }

        public void LongSubtract()
        {
            this.m_Writer.WriteLine("lsub");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void LongMultiply()
        {
            this.m_Writer.WriteLine("lmul");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void LongDivide()
        {
            this.m_Writer.WriteLine("ldiv");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void LongModulo()
        {
            this.m_Writer.WriteLine("lrem");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void LongNegate()
        {
            this.m_Writer.WriteLine("lneg");
        }

        public void LongReturn()
        {
            this.m_Writer.WriteLine("lreturn");
        }

        public void LongLoad(int local)
        {
            this.m_Writer.WriteLine("lload " + local);
            this.Push();
        }

        #endregion

        #region Float Operations

        public void FloatAdd()
        {
            this.m_Writer.WriteLine("fadd");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void FloatSubtract()
        {
            this.m_Writer.WriteLine("fsub");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void FloatMultiply()
        {
            this.m_Writer.WriteLine("fmul");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void FloatDivide()
        {
            this.m_Writer.WriteLine("fdiv");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void FloatModulo()
        {
            this.m_Writer.WriteLine("frem");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void FloatNegate()
        {
            this.m_Writer.WriteLine("fneg");
        }

        public void FloatReturn()
        {
            this.m_Writer.WriteLine("freturn");
        }

        public void FloatStore(int local)
        {
            this.m_Writer.WriteLine("fstore " + local);
            this.Pop();
        }

        public void FloatLoad(int local)
        {
            this.m_Writer.WriteLine("fload " + local);
            this.Push();
        }

        #endregion

        #region Double Operations

        public void DoubleAdd()
        {
            this.m_Writer.WriteLine("dadd");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void DoubleSubtract()
        {
            this.m_Writer.WriteLine("dsub");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void DoubleMultiply()
        {
            this.m_Writer.WriteLine("dmul");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void DoubleDivide()
        {
            this.m_Writer.WriteLine("ddiv");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void DoubleModulo()
        {
            this.m_Writer.WriteLine("drem");
            this.Pop(); // 2 on the stack replaced by 1
        }

        public void DoubleNegate()
        {
            this.m_Writer.WriteLine("dneg");
        }

        public void DoubleReturn()
        {
            this.m_Writer.WriteLine("dreturn");
        }

        public void DoubleStore(int local)
        {
            this.m_Writer.WriteLine("dstore " + local);
            this.Pop();
        }

        public void DoubleLoad(int local)
        {
            this.m_Writer.WriteLine("dload " + local);
            this.Push();
        }

        #endregion
    }
}

