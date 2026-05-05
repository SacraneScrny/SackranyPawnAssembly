using SackranyPawnAssembly.Cache;

namespace SackranyPawnAssembly.ModuleTypes
{
    public interface IAssemblyModuleType { int Id { get; } }
    public abstract class IAssemblyModuleType<TSelf> : IAssemblyModuleType 
        where TSelf : IAssemblyModuleType<TSelf>
    {
        public int Id => TypeRegistry<IAssemblyModuleType>.Id<TSelf>.Value;
    }
    public readonly struct AssemblyModuleType<TSelf> where TSelf : IAssemblyModuleType
    {
        public static readonly int Id = TypeRegistry<IAssemblyModuleType>.Id<TSelf>.Value;
    }
}