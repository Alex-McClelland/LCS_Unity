using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Containers
{
    public class Tree<T>
    {
        public List<Tree<T>> children { get; set; }
        public T value { get; set; }

        public Tree()
        {
            children = new List<Tree<T>>();
            value = default(T);
        }

        public Tree(T value)
        {
            children = new List<Tree<T>>();
            this.value = value;            
        }
    }
}
