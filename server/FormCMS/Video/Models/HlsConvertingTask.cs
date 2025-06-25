namespace FormCMS.Video.Models;

public record HlsConvertingTask(
   string StoragePath,
   string StorageFolder,
   string StorageTargetPath,

   string TempPath ="",
   string TempFolder="",
   string TempTargetPath=""
);

public static class HlsConvertingTaskHelper
{
   public static HlsConvertingTask CreatTask(string storagePath)
   {
      var tempPath = Path.GetTempPath().TrimEnd('/');
      var task = new HlsConvertingTask(
         StoragePath: storagePath,
         StorageFolder: storagePath + ".hls",
         StorageTargetPath:storagePath + ".hls/output.m3u8" 
      );
      
      return task with
      {
         TempPath = Path.Join(tempPath, task.StoragePath),
         TempFolder = Path.Join(tempPath, task.StorageFolder),
         TempTargetPath = Path.Join(tempPath, task.StorageTargetPath),
      };
   }
}