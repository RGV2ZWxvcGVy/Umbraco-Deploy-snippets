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
    public class MediaComponentGridCellValueConnector : DefaultGridCellValueConnector
    {
        private readonly ILogger<MediaComponentGridCellValueConnector> _logger;

        public MediaComponentGridCellValueConnector(IEntityService entityService, ILocalLinkParser localLinkParser, ILogger<MediaComponentGridCellValueConnector> logger)
            : base(entityService, localLinkParser)
        {
            _logger = logger;
        }

        // Location of default Umbraco grid editors: /wwwroot/umbraco/views/propertyeditors/grid/editors/[editor].html
        public override bool IsConnector(string view) => string.Equals(view, "/App_Plugins/Grid/Picture/views/image.html", StringComparison.OrdinalIgnoreCase);

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
                Udi mediaUdi = json.udi;
                if (mediaUdi != null)
                {
                    // For some reason Umbraco Deploy expects an udi as id, so we overwrite the integer id with a string udi here
                    json.id = mediaUdi.ToString();
                    control.Value = JToken.FromObject(json);
                    dependencies.Add(new ArtifactDependency(mediaUdi, false, ArtifactDependencyMode.Exist));
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
                var mediaUdi = UdiParser.Parse(json.id!.ToString() ?? string.Empty) as GuidUdi;
                if (mediaUdi != null)
                {
                    var nodeId = GetNodeId(mediaUdi, contextCache);
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
