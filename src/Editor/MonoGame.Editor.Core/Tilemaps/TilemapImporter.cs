using System.Xml.Linq;

namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>Loads and saves Tiled .tmx files as <see cref="EditorTilemapAsset"/> instances.</summary>
public static class TilemapImporter
{
    /// <summary>Loads a .tmx file from disk.</summary>
    /// <param name="tmxPath">Absolute path to the .tmx file.</param>
    public static EditorTilemapAsset Load(string tmxPath)
    {
        var xml = XDocument.Load(tmxPath);
        return ParseMap(xml, tmxPath);
    }

    /// <summary>Parses a .tmx XML document from a string (useful for unit testing).</summary>
    /// <param name="xmlContent">TMX XML content.</param>
    /// <param name="filePath">Virtual file path used as base directory for relative asset paths.</param>
    public static EditorTilemapAsset ParseFromString(string xmlContent, string filePath = "")
    {
        var xml = XDocument.Parse(xmlContent);
        return ParseMap(xml, filePath);
    }

    /// <summary>Saves the asset back to its source .tmx file using CSV encoding.</summary>
    public static void Save(EditorTilemapAsset asset)
    {
        var xml = BuildMap(asset);
        xml.Save(asset.FilePath);
    }

    private static EditorTilemapAsset ParseMap(XDocument xml, string filePath)
    {
        var map = xml.Root!;
        int mapWidth = (int)map.Attribute("width")!;
        int mapHeight = (int)map.Attribute("height")!;
        int tileWidth = (int)map.Attribute("tilewidth")!;
        int tileHeight = (int)map.Attribute("tileheight")!;

        string baseDir = string.IsNullOrEmpty(filePath) ? "" : Path.GetDirectoryName(filePath) ?? "";

        var tilesets = new List<EditorTileset>();
        foreach (var tsEl in map.Elements("tileset"))
        {
            string? source = (string?)tsEl.Attribute("source");
            int firstGid = (int)tsEl.Attribute("firstgid")!;

            if (source is not null)
            {
                string tsxPath = Path.Combine(baseDir, source);
                if (File.Exists(tsxPath))
                {
                    var tsx = XDocument.Load(tsxPath);
                    tilesets.Add(ParseTilesetElement(tsx.Root!, firstGid));
                }
                else
                {
                    tilesets.Add(new EditorTileset
                    {
                        FirstGid = firstGid,
                        Name = Path.GetFileNameWithoutExtension(source),
                        ImagePath = string.Empty,
                        TileWidth = tileWidth,
                        TileHeight = tileHeight,
                        Columns = 1,
                        TileCount = 1,
                    });
                }
            }
            else
            {
                tilesets.Add(ParseTilesetElement(tsEl, firstGid));
            }
        }

        var layers = new List<EditorTileLayer>();
        foreach (var layerEl in map.Elements("layer"))
        {
            string layerName = (string)layerEl.Attribute("name")!;
            int layerWidth = (int)(layerEl.Attribute("width") ?? map.Attribute("width"))!;
            int layerHeight = (int)(layerEl.Attribute("height") ?? map.Attribute("height"))!;

            var layer = new EditorTileLayer(layerName, layerWidth, layerHeight);

            var dataEl = layerEl.Element("data");
            if (dataEl is not null)
            {
                string encoding = (string?)dataEl.Attribute("encoding") ?? "xml";
                if (encoding == "csv")
                    ParseCsvData(layer, dataEl.Value);
                else
                    ParseXmlData(layer, dataEl);
            }

            layers.Add(layer);
        }

        return new EditorTilemapAsset(filePath, mapWidth, mapHeight, tileWidth, tileHeight, tilesets, layers);
    }

    private static EditorTileset ParseTilesetElement(XElement tsEl, int firstGid)
    {
        string name = (string?)tsEl.Attribute("name") ?? string.Empty;
        int tileWidth = (int?)tsEl.Attribute("tilewidth") ?? 0;
        int tileHeight = (int?)tsEl.Attribute("tileheight") ?? 0;
        int columns = (int?)tsEl.Attribute("columns") ?? 1;
        int tileCount = (int?)tsEl.Attribute("tilecount") ?? 0;

        string imagePath = string.Empty;
        var imageEl = tsEl.Element("image");
        if (imageEl is not null)
            imagePath = (string?)imageEl.Attribute("source") ?? string.Empty;

        return new EditorTileset
        {
            FirstGid = firstGid,
            Name = name,
            ImagePath = imagePath,
            TileWidth = tileWidth,
            TileHeight = tileHeight,
            Columns = columns,
            TileCount = tileCount,
        };
    }

    private static void ParseCsvData(EditorTileLayer layer, string csvData)
    {
        string[] tokens = csvData.Split([',', '\n', '\r'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int col = 0, row = 0;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!int.TryParse(tokens[i], out int gid))
                continue;
            layer.SetTile(col, row, gid == 0 ? null : gid);
            col++;
            if (col >= layer.Width)
            {
                col = 0;
                row++;
            }
        }
    }

    private static void ParseXmlData(EditorTileLayer layer, XElement dataEl)
    {
        int col = 0, row = 0;
        foreach (var tileEl in dataEl.Elements("tile"))
        {
            int gid = (int?)tileEl.Attribute("gid") ?? 0;
            layer.SetTile(col, row, gid == 0 ? null : gid);
            col++;
            if (col >= layer.Width)
            {
                col = 0;
                row++;
            }
        }
    }

    private static XDocument BuildMap(EditorTilemapAsset asset)
    {
        var mapEl = new XElement("map",
            new XAttribute("version", "1.10"),
            new XAttribute("tiledversion", "1.10.2"),
            new XAttribute("orientation", "orthogonal"),
            new XAttribute("renderorder", "right-down"),
            new XAttribute("width", asset.MapWidth),
            new XAttribute("height", asset.MapHeight),
            new XAttribute("tilewidth", asset.TileWidth),
            new XAttribute("tileheight", asset.TileHeight),
            new XAttribute("infinite", "0"));

        foreach (var ts in asset.Tilesets)
        {
            var tsEl = new XElement("tileset",
                new XAttribute("firstgid", ts.FirstGid),
                new XAttribute("name", ts.Name),
                new XAttribute("tilewidth", ts.TileWidth),
                new XAttribute("tileheight", ts.TileHeight),
                new XAttribute("columns", ts.Columns),
                new XAttribute("tilecount", ts.TileCount));

            if (!string.IsNullOrEmpty(ts.ImagePath))
                tsEl.Add(new XElement("image", new XAttribute("source", ts.ImagePath)));

            mapEl.Add(tsEl);
        }

        foreach (var layer in asset.Layers)
        {
            var layerEl = new XElement("layer",
                new XAttribute("name", layer.Name),
                new XAttribute("width", layer.Width),
                new XAttribute("height", layer.Height));

            layerEl.Add(new XElement("data",
                new XAttribute("encoding", "csv"),
                layer.ToCsvData()));

            mapEl.Add(layerEl);
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), mapEl);
    }
}
