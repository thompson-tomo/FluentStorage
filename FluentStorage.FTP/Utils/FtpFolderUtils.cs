using FluentFTP;
using FluentStorage.Enums;
using FluentStorage.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentStorage.FTP.Utils {
	public static class FtpFolderUtils {

		public static Dictionary<StorageExistsMode, FtpRemoteExists> UploadFolderMap = new Dictionary<StorageExistsMode, FtpRemoteExists> {
			[StorageExistsMode.Skip] = FtpRemoteExists.Skip,
			[StorageExistsMode.Overwrite] = FtpRemoteExists.Overwrite,
		};
		public static Dictionary<StorageExistsMode, FtpLocalExists> DownloadFolderMap = new Dictionary<StorageExistsMode, FtpLocalExists> {
			[StorageExistsMode.Skip] = FtpLocalExists.Skip,
			[StorageExistsMode.Overwrite] = FtpLocalExists.Overwrite,
		};

		public static StorageProgress ConvertProgress(FtpProgress progress) {
			return new StorageProgress {
				Progress = progress.Progress,
				TransferredBytes = progress.TransferredBytes,
				TransferSpeed = progress.TransferSpeed,
				ETA = progress.ETA,
				LocalPath = progress.LocalPath,
				RemotePath = progress.RemotePath,
				FileIndex = progress.FileIndex,
				FileCount = progress.FileCount
			};
		}


	}
}
