using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LeafNetCore
{
    /// <summary>
    /// 反射缓存，根据名字获取对应的类型，其中基类必须为T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ClassMapper<T> where T : class
    {
        private Dictionary<string, T> cache = new Dictionary<string, T>();
        string suffixString;
        string namespaceString;
        //Assembly assembly;
        Assembly[] assemblies;

        public ClassMapper(string suffixString = "", string namespaceString = "")
        {
            this.suffixString = suffixString;
            this.namespaceString = namespaceString;

            //加载程序集
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        /// <summary>
        /// 获取字符串对应的类，自动补充后缀
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetClass(string name)
        {
            string className = namespaceString + name + suffixString;
            if (cache.ContainsKey(className) == false)
            {
                Type type = null;
                foreach (Assembly assembly in assemblies)
                {
                    type = assembly.GetType(className);
                    // 对获取到的类型进行处理
                    if (type != null)
                    {
                        break;
                    }
                }
                T instance = Activator.CreateInstance(type) as T;
                cache[className] = instance;
            }
            return cache[className];
        }

        /// <summary>
        /// 获取protobuf的反序列化器
        /// </summary>
        /// <param name="genericTypeName"></param>
        /// <returns></returns>
        public T GetParser(string genericTypeName)
        {
            string className = namespaceString + genericTypeName + suffixString;
            if (cache.ContainsKey(className) == false)
            {
                //Type t = typeof(HeartMsg);

                // 获取类的 Type 对象
                Type msgType = null;
                foreach (Assembly assembly in assemblies)
                {
                    msgType = assembly.GetType(className);
                    // 对获取到的类型进行处理
                    if (msgType != null)
                    {
                        break;
                    }
                }
                if (msgType == null)
                {
                    cache[className] = null;
                }
                else
                {
                    // 获取属性 PropertyInfo 对象
                    PropertyInfo parserProperty = msgType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
                    // 获取属性的值
                    object parserValue = parserProperty.GetValue(null);
                    cache[className] = parserValue as T;
                }
            }
            return cache[className];
        }
    }
}
