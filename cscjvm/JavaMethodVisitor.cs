using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace cscjvm
{
    public class JavaMethodVisitor : DepthFirstAstVisitor
    {
        private CSharpAstResolver m_Resolver;
        private StreamWriter m_Writer;
        private Dictionary<string, int> m_Locals = new Dictionary<string, int>();
        private JavaBytecodeWriter m_BytecodeWriter;

        public JavaMethodVisitor(CSharpAstResolver resolver, StreamWriter writer)
        {
            this.m_Resolver = resolver;
            this.m_Writer = writer;
            this.m_BytecodeWriter = new JavaBytecodeWriter(writer);
            this.m_Locals.Add("~dummy0", 0);
            this.m_Locals.Add("~dummy1", 1);
        }

        public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            base.VisitExpressionStatement(expressionStatement);
        }

        public override void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            base.VisitInvocationExpression(invocationExpression);

            var resolve = this.m_Resolver.Resolve(invocationExpression.Target);

            if (resolve is MethodGroupResolveResult)
            {
                var methodResolve = resolve as MethodGroupResolveResult;
                var method = methodResolve.Methods.First(x => x.Name == methodResolve.MethodName);
                this.m_Writer.Write(JavaInvoke.DetermineInvocationMethod(method));
                this.m_Writer.Write(' ');
                this.m_Writer.WriteLine(JavaSignature.CreateMethodSignature(method));
            }
            else
                throw new NotSupportedException();
        }

        public override void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
        {
            base.VisitPrimitiveExpression(primitiveExpression);
            
            var resolve = this.m_Resolver.Resolve(primitiveExpression);
            if (resolve is ConstantResolveResult)
            {
                var constantResolve = resolve as ConstantResolveResult;
                if (constantResolve.Type.FullName == typeof(string).FullName)
                    this.m_BytecodeWriter.LoadConstant(constantResolve.ConstantValue as string);
                else if (constantResolve.Type.FullName == typeof(int).FullName)
                    this.m_BytecodeWriter.LoadConstant((int)constantResolve.ConstantValue);
                else if (constantResolve.Type.FullName == typeof(float).FullName)
                    this.m_BytecodeWriter.LoadConstant((float)constantResolve.ConstantValue);
                else if (constantResolve.Type.FullName == typeof(long).FullName)
                    this.m_BytecodeWriter.LoadConstant((long)constantResolve.ConstantValue);
                else if (constantResolve.Type.FullName == typeof(double).FullName)
                    this.m_BytecodeWriter.LoadConstant((double)constantResolve.ConstantValue);
            }
            else
                throw new NotSupportedException();
        }

        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression);

            var resolve = this.m_Resolver.Resolve(memberReferenceExpression);

            if (resolve is MemberResolveResult)
            {
                var memberResolve = resolve as MemberResolveResult;
                if (memberResolve.TargetResult is TypeResolveResult)
                {
                    this.m_BytecodeWriter.MemberGetStatic(memberResolve.Member);
                }
                else
                    throw new NotSupportedException();
            }
            else if (resolve is MethodGroupResolveResult)
            {
                // FIXME: Should we do something here?  I think this is only really useful
                // when the parent is not an invocation expression, e.g. we're referencing the
                // method itself to pass around as a delegate.
                if (!(memberReferenceExpression.Parent is InvocationExpression))
                    throw new NotSupportedException();
            }
            else if (resolve is NamespaceResolveResult)
            {
                // Seems to occur when referencing a namespace of a namespace.  I don't
                // think there's any relevant compiler output for this.
            }
            else if (resolve is TypeResolveResult)
            {
                // FIXME: Seems to occur when referencing a "member" of a namespace.
            }
            else
                throw new NotSupportedException();
        }

        public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            base.VisitMethodDeclaration(methodDeclaration);
            this.m_Writer.WriteLine(".limit stack " + this.m_BytecodeWriter.MaxStack);
            this.m_Writer.WriteLine(".limit locals " + this.m_Locals.Count);
            this.m_Writer.WriteLine("return");
        }

        public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            foreach (var variable in variableDeclarationStatement.Variables)
            {
                this.m_Locals.Add(variable.Name, this.GetFreeVariableIndex());
            }

            base.VisitVariableDeclarationStatement(variableDeclarationStatement);
        }

        public override void VisitVariableInitializer(VariableInitializer variableInitializer)
        {
            base.VisitVariableInitializer(variableInitializer);

            var resolve = this.m_Resolver.Resolve(variableInitializer.Initializer);
            if (resolve is TypeResolveResult ||
                resolve is ConstantResolveResult ||
                resolve is LocalResolveResult ||
                resolve is OperatorResolveResult)
            {
                if (this.m_Locals.ContainsKey(variableInitializer.Name))
                {
                    if (resolve.Type.FullName == typeof(int).FullName)
                        this.m_BytecodeWriter.IntStore(this.m_Locals[variableInitializer.Name]);
                    else if (resolve.Type.FullName == typeof(long).FullName)
                        this.m_BytecodeWriter.LongStore(this.m_Locals[variableInitializer.Name]);
                    else if (resolve.Type.FullName == typeof(float).FullName)
                        this.m_BytecodeWriter.FloatStore(this.m_Locals[variableInitializer.Name]);
                    else if (resolve.Type.FullName == typeof(double).FullName)
                        this.m_BytecodeWriter.DoubleStore(this.m_Locals[variableInitializer.Name]);
                    else 
                        this.m_BytecodeWriter.ReferenceStore(this.m_Locals[variableInitializer.Name]);
                }
            }
            else if (resolve is NamespaceResolveResult)
            {
            }
            else
                throw new NotSupportedException();
        }

        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            var resolve = this.m_Resolver.Resolve(identifierExpression);
            if (resolve is TypeResolveResult ||
                resolve is ConstantResolveResult ||
                resolve is LocalResolveResult ||
                resolve is OperatorResolveResult)
            {
                if (this.m_Locals.ContainsKey(identifierExpression.Identifier))
                {
                    if (resolve.Type.FullName == typeof(int).FullName)
                        this.m_BytecodeWriter.IntLoad(this.m_Locals[identifierExpression.Identifier]);
                    else if (resolve.Type.FullName == typeof(long).FullName)
                        this.m_BytecodeWriter.LongLoad(this.m_Locals[identifierExpression.Identifier]);
                    else if (resolve.Type.FullName == typeof(float).FullName)
                        this.m_BytecodeWriter.FloatLoad(this.m_Locals[identifierExpression.Identifier]);
                    else if (resolve.Type.FullName == typeof(double).FullName)
                        this.m_BytecodeWriter.DoubleLoad(this.m_Locals[identifierExpression.Identifier]);
                    else 
                        this.m_BytecodeWriter.ReferenceLoad(this.m_Locals[identifierExpression.Identifier]);
                }
            }
            else if (resolve is NamespaceResolveResult)
            {
            }
            else
                throw new NotSupportedException();
        }

        public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            base.VisitBinaryOperatorExpression(binaryOperatorExpression);

            var left = binaryOperatorExpression.Left;
            var right = binaryOperatorExpression.Right;
            var resolveLeft = this.m_Resolver.Resolve(left);
            var resolveRight = this.m_Resolver.Resolve(right);
            IType leftType = null, rightType = null;

            if (resolveLeft is TypeResolveResult ||
                resolveLeft is ConstantResolveResult ||
                resolveLeft is LocalResolveResult ||
                resolveLeft is OperatorResolveResult)
                leftType = resolveLeft.Type;
            if (resolveRight is TypeResolveResult ||
                resolveRight is ConstantResolveResult ||
                resolveRight is LocalResolveResult ||
                resolveRight is OperatorResolveResult)
                rightType = resolveRight.Type;

            if (leftType != null && rightType != null)
            {
                if (leftType.FullName == typeof(int).FullName && rightType.FullName == typeof(int).FullName)
                {
                    switch (binaryOperatorExpression.Operator)
                    {
                        case BinaryOperatorType.Add:
                            this.m_BytecodeWriter.IntAdd();
                            break;
                        case BinaryOperatorType.Subtract:
                            this.m_BytecodeWriter.IntSubtract();
                            break;
                        case BinaryOperatorType.Multiply:
                            this.m_BytecodeWriter.IntMultiply();
                            break;
                        case BinaryOperatorType.Divide:
                            this.m_BytecodeWriter.IntDivide();
                            break;
                    }
                }
                else if (leftType.FullName == typeof(long).FullName && rightType.FullName == typeof(long).FullName)
                {
                    switch (binaryOperatorExpression.Operator)
                    {
                        case BinaryOperatorType.Add:
                            this.m_BytecodeWriter.LongAdd();
                            break;
                        case BinaryOperatorType.Subtract:
                            this.m_BytecodeWriter.LongSubtract();
                            break;
                        case BinaryOperatorType.Multiply:
                            this.m_BytecodeWriter.LongMultiply();
                            break;
                        case BinaryOperatorType.Divide:
                            this.m_BytecodeWriter.LongDivide();
                            break;
                    }
                }
                else if (leftType.FullName == typeof(float).FullName && rightType.FullName == typeof(float).FullName)
                {
                    switch (binaryOperatorExpression.Operator)
                    {
                        case BinaryOperatorType.Add:
                            this.m_BytecodeWriter.FloatAdd();
                            break;
                        case BinaryOperatorType.Subtract:
                            this.m_BytecodeWriter.FloatSubtract();
                            break;
                        case BinaryOperatorType.Multiply:
                            this.m_BytecodeWriter.FloatMultiply();
                            break;
                        case BinaryOperatorType.Divide:
                            this.m_BytecodeWriter.FloatDivide();
                            break;
                    }
                }
                else if (leftType.FullName == typeof(double).FullName && rightType.FullName == typeof(double).FullName)
                {
                    switch (binaryOperatorExpression.Operator)
                    {
                        case BinaryOperatorType.Add:
                            this.m_BytecodeWriter.DoubleAdd();
                            break;
                        case BinaryOperatorType.Subtract:
                            this.m_BytecodeWriter.DoubleSubtract();
                            break;
                        case BinaryOperatorType.Multiply:
                            this.m_BytecodeWriter.DoubleMultiply();
                            break;
                        case BinaryOperatorType.Divide:
                            this.m_BytecodeWriter.DoubleDivide();
                            break;
                    }
                }
                else if (leftType.FullName == rightType.FullName)
                {
                    // Reference operator.  In C# we can operator overload; we can't do that in Java.  So our
                    // operator overloads in C# are mapped to static functions called:
                    //   _add
                    //   _sub
                    //   _mul
                    //   _div
                    // etc.
                    throw new NotImplementedException();
                }

            }
            else
                throw new NotSupportedException();
        }

        private int GetFreeVariableIndex()
        {
            return this.m_Locals.Count;
        }
    }
}

