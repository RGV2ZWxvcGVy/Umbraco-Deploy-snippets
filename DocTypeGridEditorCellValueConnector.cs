using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Connectors.GridCellValueConnectors;
using Umbraco.Cms.Web.Common.PublishedModels;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace ProjectName.Core.Deploy.Connectors
{
    // The intention is to always use the methods of the base class, unless there is a dependency.
    // If there is a dependency, such as an image that needs to be included, it must be registered.
    // This can be done by using: dependencies.Add(new ArtifactDependency(udi, ordering: false, ArtifactDependencyMode.Exist));

    /// <summary>
    /// Custom value connector for the Doc Type Grid Editor.
    /// </summary>
    public class DocTypeGridEditorCellValueConnector : DefaultGridCellValueConnector
    {
        private readonly ILogger<DocTypeGridEditorCellValueConnector> _logger;
        // The document types that use this value connector, and contain dependencies
        private readonly string[] _docTypes = { ModelName.ModelTypeAlias };
        // The media criteria that indicates if there is a media dependency
        private readonly string[] _mediaCriteria = { "\"image\":", "\"key\":", "\"mediaKey\":", "\"crops\":", "\"focalPoint\":" };

        public DocTypeGridEditorCellValueConnector(IEntityService entityService, ILocalLinkParser localLinkParser, ILogger<DocTypeGridEditorCellValueConnector> logger)
            : base(entityService, localLinkParser)
        {
            _logger = logger;
        }

        public override bool IsConnector(string view) => string.Equals(view, "/App_Plugins/DocTypeGridEditor/Views/DocTypeGridEditor.html", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the value of the grid control, and adds the required dependencies, if any.
        /// This method is typically used when a node is saved or when transfering content between environments.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="dependencies"></param>
        /// <param name="contextCache"></param>
        /// <returns></returns>
        public override string GetValue(GridValue.GridControl control, ICollection<ArtifactDependency> dependencies, IContextCache contextCache)
        {
            // Note that if there is a parent node containing doc type grid editor content, the parent node value is evaluated first
            var value = control.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            // If the value does not contain one of the doc type aliases that are required or all of the media criteria, continue the method as if there are no dependencies
            if (!_docTypes.Any(value.Contains) || !_mediaCriteria.All(value.Contains))
            {
                return base.GetValue(control, dependencies, contextCache);
            }

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);
                string mediaKey = json.value?.image[0]?.mediaKey;
                if (!string.IsNullOrEmpty(mediaKey))
                {
                    var guid = new Guid(mediaKey);
                    var udi = Udi.Create(Constants.UdiEntityType.Media, guid);
                    dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));
                }
            }
            catch (JsonReaderException innerException)
            {
                _logger.LogError(innerException, "Invalid JSON value for grid editor: '{editor}'.", control.Editor.Alias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not add the image dependency for grid editor: '{editor}'.", control.Editor.Alias);
            }

            return base.GetValue(control, dependencies, contextCache);
        }

        /// <summary>
        /// Sets the value of the grid control, and adds the required dependencies, if any.
        /// This method is typically used when restoring content from an environment.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="contextCache"></param>
        public override void SetValue(GridValue.GridControl control, IContextCache contextCache)
        {
            base.SetValue(control, contextCache);
        }
    }
}
