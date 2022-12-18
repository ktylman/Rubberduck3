﻿using Rubberduck.VBEditor.ComManagement;

namespace Rubberduck.VBEditor.SourceCodeHandling
{
    public class ComponentSourceCodeHandlerSourceCodeHandlerAdapter : ISourceCodeHandler<string>
    {
        private readonly IComponentSourceCodeHandler _componentSourceCodeHandler;
        private readonly IProjectsProvider _projectsProvider;

        public ComponentSourceCodeHandlerSourceCodeHandlerAdapter(IComponentSourceCodeHandler componentSourceCodeHandler, IProjectsProvider projectsProvider)
        {
            _componentSourceCodeHandler = componentSourceCodeHandler;
            _projectsProvider = projectsProvider;
        }

        public CodeString GetCurrentLogicalLine(QualifiedModuleName module)
        {
            throw new System.NotSupportedException();
        }

        public void SetSelection(QualifiedModuleName module, Selection selection)
        {
            throw new System.NotSupportedException();
        }

        public string SourceCode(QualifiedModuleName module)
        {
            var component = _projectsProvider.Component(module);
            if (component == null)
            {
                return string.Empty;
            }

            return _componentSourceCodeHandler.SourceCode(component);
        }

        public string StringSource(QualifiedModuleName module)
        {
            throw new System.NotSupportedException();
        }

        public void SubstituteCode(QualifiedModuleName module, string newCode)
        {
            var component = _projectsProvider.Component(module);
            if (component == null)
            {
                return;
            }

            using (_componentSourceCodeHandler.SubstituteCode(component, newCode)){} //We do nothing; we just need to guarantee that the returned SCW gets disposed.
        }

        public void SubstituteCode(QualifiedModuleName module, CodeString newCode)
        {
            throw new System.NotSupportedException();
        }
    }
}