﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark.Compiler.CSharp.ChunkVisitors;

namespace Spark.Compiler.CSharp
{
    public class DefaultViewCompiler : ViewCompiler
    {
        public override void CompileView(IEnumerable<IList<Chunk>> viewTemplates, IEnumerable<IList<Chunk>> allResources)
        {
            GenerateSourceCode(viewTemplates, allResources);

            var batchCompiler = new BatchCompiler();
            var assembly = batchCompiler.Compile(Debug, SourceCode);
            CompiledType = assembly.GetType(ViewClassFullName);
        }


        public override void GenerateSourceCode(IEnumerable<IList<Chunk>> viewTemplates, IEnumerable<IList<Chunk>> allResources)
        {
            var source = new StringBuilder();
            var usingGenerator = new UsingNamespaceVisitor(source);
            var baseClassGenerator = new BaseClassVisitor { BaseClass = BaseClass };
            var globalsGenerator = new GlobalMembersVisitor(source);
            var viewGenerator = new GeneratedCodeVisitor(source) { Indent = 8 };


            // using <namespaces>;
            foreach (var ns in UseNamespaces ?? new string[0])
                usingGenerator.UsingNamespace(ns);

            foreach (var assembly in UseAssemblies ?? new string[0])
                usingGenerator.UsingAssembly(assembly);

            foreach (var resource in allResources)
                usingGenerator.Accept(resource);

            foreach (var resource in allResources)
                baseClassGenerator.Accept(resource);

            var viewClassName = "View" + GeneratedViewId.ToString("n");

            if (string.IsNullOrEmpty(TargetNamespace))
            {
                ViewClassFullName = viewClassName;
            }
            else
            {
                ViewClassFullName = TargetNamespace + "." + viewClassName;

                source.AppendLine();
                source.AppendLine(string.Format("namespace {0}", TargetNamespace));
                source.AppendLine("{");
            }

            source.AppendLine();

            if (Descriptor != null)
            {
                // [SparkView] attribute
                source.AppendLine("[global::Spark.SparkViewAttribute(");
                if (TargetNamespace != null)
                    source.AppendFormat("    TargetNamespace=\"{0}\",", TargetNamespace).AppendLine();
                source.AppendLine("    Templates = new string[] {");
                source.Append("      ").AppendLine(string.Join(",\r\n      ",
                                                               Descriptor.Templates.Select(
                                                                   t => "\"" + t.Replace("\\", "\\\\") + "\"").ToArray()));
                source.AppendLine("    })]");
            }

            // public class ViewName : BasePageType 
            source.AppendLine(string.Format("public class {0} : {1}", viewClassName, baseClassGenerator.BaseClassTypeName));
            source.AppendLine("{");

            source.AppendLine();
            source.AppendLine("    public override System.Guid GeneratedViewId");
            source.AppendLine(string.Format("    {{ get {{ return new System.Guid(\"{0:n}\"); }} }}", GeneratedViewId));

            if (Descriptor != null && Descriptor.Accessors != null)
            {
                foreach (var accessor in Descriptor.Accessors)
                {
                    source.AppendLine();
                    source.Append("    public ").AppendLine(accessor.Property);
                    source.Append("    { get { return ").Append(accessor.GetValue).AppendLine("; } }");
                }
            }

            // properties and macros
            foreach (var resource in allResources)
                globalsGenerator.Accept(resource);

            // public void RenderViewLevelx()
            int renderLevel = 0;
            foreach (var viewTemplate in viewTemplates)
            {
                source.AppendLine();
                source.AppendLine(string.Format("    public void RenderViewLevel{0}()", renderLevel));
                source.AppendLine("    {");
                viewGenerator.Accept(viewTemplate);
                source.AppendLine("    }");
                ++renderLevel;
            }

            // public void RenderView()
            source.AppendLine();
            source.AppendLine("    public override void RenderView(System.IO.TextWriter writer)");
            source.AppendLine("    {");
            for (int invokeLevel = 0; invokeLevel != renderLevel; ++invokeLevel)
            {
                if (invokeLevel != renderLevel - 1)
                {
                    source.AppendLine(string.Format("        using (OutputScope(new System.IO.StringWriter())) {{RenderViewLevel{0}(); Content[\"view\"] = Output;}}", invokeLevel));
                }
                else
                {
                    source.AppendLine(string.Format("        using (OutputScope(writer)) {{RenderViewLevel{0}();}}", invokeLevel));
                }
            }
            source.AppendLine("    }");

            source.AppendLine("}");

            if (!string.IsNullOrEmpty(TargetNamespace))
            {
                source.AppendLine("}");
            }

            SourceCode = source.ToString();
        }
    }
}