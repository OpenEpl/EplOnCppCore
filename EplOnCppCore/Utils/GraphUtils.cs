using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core.Utils
{
    public class GraphUtils
    {
        public GraphUtils()
        {
            throw new NotSupportedException();
        }

        public static string WriteGraphvizScript<TVertex, TEdge>(IVertexAndEdgeListGraph<TVertex, TEdge> graph, string name) where TEdge : IEdge<TVertex>
        {
            var script = new StringBuilder();
            script.Append(graph.IsDirected ? "digraph" : "graph")
                .Append(" ")
                .Append(name)
                .AppendLine("{");
            foreach (var vertex in graph.Vertices)
            {
                script.Append("\"")
                    .Append(vertex)
                    .Append("\"")
                    .Append(graph.IsDirected ? "->" : "--")
                    .Append("{")
                    .Append(string.Join(" ", graph.OutEdges(vertex).Select(x => $"\"{x.Target}\"")))
                    .Append("}")
                    .AppendLine();
            }
            script.AppendLine("}");
            return script.ToString();
        }

        public static void AnalyzeDependencies<TVertex, TEdge>(IVertexAndEdgeListGraph<TVertex, TEdge> graph, TVertex root, HashSet<TVertex> dependencies) where TEdge : IEdge<TVertex>
        {
            if (dependencies.Contains(root))
                return;
            dependencies.Add(root);
            foreach (var item in graph.OutEdges(root).Select(x => x.Target))
            {
                AnalyzeDependencies(graph, item, dependencies);
            }
        }
    }
}