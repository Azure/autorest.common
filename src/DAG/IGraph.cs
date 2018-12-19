using System;
using System.Collections.Generic;
using System.Text;

namespace AutoRest.Core.Model
{
    public interface IGraph<DataT, NodeT> where NodeT : INode<DataT, NodeT>
    {
        // The nodes in the graph.
        SortedDictionary<string, NodeT> nodeTable { get; }

        /**
         * @return all nodes in the graph.
         */
        SortedDictionary<string, NodeT>.ValueCollection getNodes();

        /**
         * Adds a node to this graph.
         *
         * @param node the node
         */
        void addNode(NodeT node);

        /**
        * Perform DFS visit in this graph.
        *
        * The directed graph will be traversed in DFS order and the visitor will be notified as
        * search explores each node and edge.
        *
        * @param visitor the graph visitor
        */
        void visit(IVisitor<DataT, NodeT> visitor);

        string findPath(string start, string end);
    }
}
