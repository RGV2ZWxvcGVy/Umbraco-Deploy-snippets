using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Connectors.GridCellValueConnectors;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ProjectName.Core.Deploy.Connectors
{
    /// <summary>
    /// Custom value connector for the Image Component.
    /// </summary>
    public class ImageComponentGridCellValueConnector : DefaultGridCellValueConnector
    {
        private readonly ILogger<ImageComponentGridCellValueConnector> _logger;

        public ImageComponentGridCellValueConnector(IEntityService entityService, ILocalLinkParser localLinkParser, ILogger<ImageComponentGridCellValueConnector> logger)
            : base(entityService, localLinkParser)
        {
            _logger = logger;
        }

        // Location of default Umbraco grid editors: /wwwroot/umbraco/views/propertyeditors/grid/editors/[editor].html
        public override bool IsConnector(string view) => string.Equals(view, "/App_Plugins/Grid/Picture/views/picture.html", StringComparison.OrdinalIgnoreCase);

        public override string GetValue(GridValue.GridControl control, ICollection<ArtifactDependency> dependencies, IContextCache contextCache)
        {
            var value = control.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);
                Udi imageUdi = json.udi;
                if (imageUdi != null)
                {
                    // For some reason Umbraco Deploy expects an udi as id, so we overwrite the integer id with a string udi here
                    json.id = imageUdi.ToString();
                    control.Value = JToken.FromObject(json);
                    dependencies.Add(new ArtifactDependency(imageUdi, false, ArtifactDependencyMode.Exist));
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

        public override void SetValue(GridValue.GridControl control, IContextCache contextCache)
        {
            var value = control.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);
                var imageUdi = UdiParser.Parse(json.id!.ToString() ?? string.Empty) as GuidUdi;
                if (imageUdi != null)
                {
                    var nodeId = GetNodeId(imageUdi, contextCache);
                    if (nodeId.HasValue)
                    {
                        // Within the 'GetValue' method we converted the int id to an udi, here we convert it back
                        json.id = nodeId.Value;
                        control.Value = JToken.FromObject(json);
                    }
                }
            }
            catch (JsonReaderException innerException)
            {
                _logger.LogError(innerException, "Invalid JSON value for grid editor: '{editor}'.", control.Editor.Alias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not convert udi to id for grid editor: '{editor}'.", control.Editor.Alias);
            }
        }
    }
}
