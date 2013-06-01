using Mono.Cecil;

namespace tmcs.Core
{
    public interface ITypeVisitor
    {
        void Initialize(TypeDefinition type);
        void VisitMethod(MethodDefinition method);
        void VisitProperty(PropertyReference property);
        void VisitField(FieldDefinition field);
    }
}

