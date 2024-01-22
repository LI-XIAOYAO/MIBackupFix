using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

internal partial class Program
{
    private static (string Package, string Type, int Feature, int BakType)[] _sysAppNames = new[]
    {
        ( "com.android.mms", "短信", 1, 1),
        ( "com.android.mms", "彩信", 2, 1),
        ( "com.android.mms", "短信设置", 10, 1),
        ( "com.android.contacts", "通话记录", 1, 1),
        ( "com.android.contacts", "联系人", 2, 1),
        ( "com.android.contacts", "通讯录与拨号", 10, 1),
        ( "com.android.browser", "", -1, 1 ),
        ( "com.miui.weather2", "", -1, 1 ),
        ( "com.android.camera", "", -1, 1 ),
        ( "com.android.settings", "设置", 1, 1),
        ( "com.android.settings", "WLAN设置", 2, 1),
        ( "com.android.phone", "电话设置", 1, 1),
        ( "com.android.deskclock", "", -1, 1 ),
        ( "com.miui.notes", "", -1, 1 ),
        ( "com.miui.securitycenter", "骚扰拦截", 2, 1),
        ( "com.miui.cleanmaster", "", -1 , 1),
        ( "com.android.calendar", "", -1 , 1),
        ( "com.xiaomi.market", "", -1 , 1),
        ( "com.miui.gallery", "", -1 , 1),
        ( "com.android.thememanager", "", -1 , 1),
        ( "com.miui.yellowpage", "", -1 , 1),
        ( "com.miui.touchassistant", "", -1 , 1),
        ( "com.miui.aod", "", -1 , 1),
        ( "com.miui.home", "", -1 , 1)
    };

    private static string[] _compressNames = new[]
    {
        "_image",
        "_video",
        "_doc",
    };

    private static void Main(string[] args)
    {
        Console.WriteLine("Find file...");

        var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.bak");

        Console.WriteLine($"File: {files.Length}");

        var compressFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.zip");
        Console.WriteLine($"CompressFile: {compressFiles.Length}");

        var bakFiles = files.Union(compressFiles).ToList();
        if (0 == bakFiles.Count)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Processing...");

        long size = 0;
        var packSb = new StringBuilder();
        foreach (var file in bakFiles)
        {
            var fileInfo = new FileInfo(file);
            size += fileInfo.Length;

            if (".ZIP".Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
            {
                CreateCompressXml(fileInfo, packSb);

                continue;
            }

            string packName = string.Empty;
            var match = PackageRegex().Match(fileInfo.Name);
            if (match.Success)
            {
                packName = match.Groups[2].Value;
            }

            int feature = 102;
            int bakType = 2;
            var pack = _sysAppNames.Where(c => c.Package.Equals(packName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (pack.Any())
            {
                var packType = pack.FirstOrDefault(c => c.Type.Equals(match.Groups[1].Value, StringComparison.OrdinalIgnoreCase));

                if (null != packType.Type)
                {
                    feature = packType.Feature;
                    bakType = packType.BakType;
                }
                else
                {
                    feature = -1;
                    bakType = 1;
                }
            }

            packSb.Append(CreatePackageXml(packName, fileInfo.Name, feature, bakType, fileInfo.Length, false));
        }

        try
        {
            File.WriteAllText("descript.xml", CreateXml(packSb, size).ToString());

            Console.WriteLine();
            Console.WriteLine($"Fix completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.ReadKey();
    }

    /// <summary>
    /// CreatePackageXml
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="bakFile"></param>
    /// <param name="feature"></param>
    /// <param name="bakType"></param>
    /// <param name="pkgSize"></param>
    /// <param name="isFile"></param>
    /// <returns></returns>
    private static string CreatePackageXml(string packageName, string bakFile, int feature, int bakType, long pkgSize, bool isFile)
    {
        return $"<package><packageName>{packageName}</packageName><feature>{feature}</feature>{(isFile ? bakFile : $"<bakFile>{bakFile}</bakFile>")}<bakType>{bakType}</bakType><pkgSize>{pkgSize}</pkgSize><sdSize>0</sdSize><state>1</state><completedSize>{pkgSize}</completedSize><error>0</error><progType>0</progType><bakFileSize>{(isFile ? "0" : pkgSize)}</bakFileSize><transingCompletedSize>0</transingCompletedSize><transingTotalSize>{(isFile ? "0" : pkgSize)}</transingTotalSize><transingSdCompletedSize>0</transingSdCompletedSize><sectionSize>0</sectionSize><sendingIndex>0</sendingIndex></package>";
    }

    /// <summary>
    /// CreateXml
    /// </summary>
    /// <param name="packSb"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    private static StringBuilder CreateXml(StringBuilder packSb, long size)
    {
        return new StringBuilder($"<?xml version='1.0' encoding='UTF-8' standalone='yes' ?><MIUI-backup><jsonMsg></jsonMsg><bakVersion>2</bakVersion><brState>3</brState><autoBackup>false</autoBackup><device>MIBackupFix</device><miuiVersion>MIBackupFix</miuiVersion><date>{DateTimeOffset.Now.ToUnixTimeMilliseconds()}</date><size>{size}</size><storageLeft>{size}</storageLeft><supportReconnect>true</supportReconnect><autoRetransferCnt>0</autoRetransferCnt><transRealCompletedSize>0</transRealCompletedSize><packages>{packSb}</packages><filesModifyTime></filesModifyTime></MIUI-backup>");
    }

    /// <summary>
    /// CreateCompressXml
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="packSb"></param>
    private static void CreateCompressXml(FileInfo fileInfo, StringBuilder packSb)
    {
        if (_compressNames.Any(c => Path.GetFileNameWithoutExtension(fileInfo.Name).EndsWith(c, StringComparison.OrdinalIgnoreCase)))
        {
            var bakFileSb = CreateCompressXml(fileInfo.FullName);
            if (null != bakFileSb)
            {
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                int feature = 0;

                if (fileName.EndsWith(_compressNames[0], StringComparison.OrdinalIgnoreCase))
                {
                    feature = 5;
                }
                else if (fileName.EndsWith(_compressNames[1], StringComparison.OrdinalIgnoreCase))
                {
                    feature = 7;
                }
                else if (fileName.EndsWith(_compressNames[2], StringComparison.OrdinalIgnoreCase))
                {
                    feature = 8;
                }

                packSb.Append(CreatePackageXml("files_for_backup", bakFileSb.ToString(), feature, feature, fileInfo.Length, true));
            }
        }
    }

    /// <summary>
    /// CreateCompressXml
    /// </summary>
    /// <param name="archiveFileName"></param>
    /// <returns></returns>
    private static StringBuilder? CreateCompressXml(string archiveFileName)
    {
        StringBuilder? bakFileSb = null;

        try
        {
            var zipArchive = ZipFile.OpenRead(archiveFileName);
            if (zipArchive.Entries.Any())
            {
                bakFileSb = new StringBuilder();

                foreach (var item in zipArchive.Entries)
                {
                    bakFileSb.Append($"<bakFile>/storage/emulated/0{item.FullName}</bakFile>");
                }
            }
        }
        catch
        {
            Console.WriteLine($"\u001b[1m\u001b[31mERROR:\u001b[39m\u001b[22m Process {Path.GetFileName(archiveFileName)} error.");
        }

        return bakFileSb;
    }

    /// <summary>
    /// PackageRegex
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("(.*?)\\((.*?)\\)")]
    private static partial Regex PackageRegex();
}