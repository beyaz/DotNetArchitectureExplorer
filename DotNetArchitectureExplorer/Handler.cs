using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace DotNetArchitectureExplorer;




static class Handler
{
    
    //        #region Public Methods
    //        public Data Handle(Data data)
    //        {
    //            if (data.SelectedText != null)
    //            {
    //                data.SelectedText = data.SelectedText.Trim();
    //            }

    //            data.OutputFileFullPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "ClassDependencyView.dgml";

    //            var assemblySearchDirectoryPath = data.AssemblySearchDirectoryPath ?? BinFolderPaths.ServerBin + "," + BinFolderPaths.ClientBin;

    //            var typeDefinition = TryToFindDefinitionAutomaticly(data);
    //            if (typeDefinition == null)
    //            {
    //                var typeFullName = UserIteraction.FindType(assemblySearchDirectoryPath);
    //                if (typeFullName.IsNullOrWhiteSpace())
    //                {
    //                    data.ErrorMessage = "Bir class seçilmelidir.";
    //                    return data;
    //                }

    //                typeDefinition = LastUsedTypes.Value.First(t => t.FullName == typeFullName).Definition;
    //            }

    //            data.DgmlFileContent = new GraphCreator().CreateGraph(typeDefinition);

    //            new FileInfo(data.OutputFileFullPath).Delete();

    //            File.WriteAllText(data.OutputFileFullPath, data.DgmlFileContent);

    //            return data;
    //        }

    //        public TypeDefinition TryToFindDefinitionAutomaticly(Data data)
    //        {
    //            var path = TryGetTargetAssemblyPath(data);

    //            if (path != null)
    //            {
    //                var resolver = new DefaultAssemblyResolver();
    //                resolver.AddSearchDirectory(data.AssemblySearchDirectoryPath);

    //                AssemblyDefinition assemblyDefinition = null;
    //                try
    //                {
    //                    assemblyDefinition = AssemblyDefinition.ReadAssembly(path, new ReaderParameters {AssemblyResolver = resolver});

    //                    var list = new List<TypeDefinition>();

    //                    assemblyDefinition.ForEachType(type =>
    //                    {
    //                        if (type.Name == data.SelectedText)
    //                        {
    //                            list.Add(type);
    //                        }
    //                    });

    //                    if (list.Count == 1)
    //                    {
    //                        return list.First();
    //                    }
    //                }
    //                catch (BadImageFormatException)
    //                {
    //                }
    //            }
    //            if (data.AssemblySearchDirectoryPath != null)
    //            {
    //                var assemblySearchDirectoryPath = data.AssemblySearchDirectoryPath;
    //                if (data.SelectedText != null)
    //                {
    //                    data.SelectedText = data.SelectedText.Trim();
    //                    var list = new BOATypeDataProvider {CountTreshold = int.MaxValue}.GetAllTypes(assemblySearchDirectoryPath).Where(t => t.Name == data.SelectedText).ToList();
    //                    if (list.Count == 1)
    //                    {
    //                        return list.First().Definition;
    //                    }
    //                }
    //            }

    //            return null;
    //        }
    //        #endregion

    //        #region Methods
    //        string TryGetTargetAssemblyPath(Data data)
    //        {
    //            if (data.ActiveProjectName == null ||
    //                data.AssemblySearchDirectoryPath == null)
    //            {
    //                return null;
    //            }

    //            var path = data.AssemblySearchDirectoryPath + data.ActiveProjectName;
    //            var fs = new FileService();
    //            var dllPath = path + ".dll";
    //            if (fs.Exists(dllPath))
    //            {
    //                return dllPath;
    //            }
    //            var exePath = path + ".exe";
    //            if (fs.Exists(exePath))
    //            {
    //                return exePath;
    //            }
    //            return null;
    //        }
    //        #endregion
}
