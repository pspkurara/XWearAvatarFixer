using global::UniVRM10;
using System;
using System.Collections.Generic;
using System.IO;
using UniGLTF;
using UnityEditor;
using UnityEngine;

namespace pspkurara.VRM10FromXRoidAvatarFixer.Editor
{

	/// <summary>
	/// VRM1.0に書き出しを行う仕組み
	/// VRM10ExportDialog.csと殆ど同じだが、GUIのロジックを取り除いてある
	/// </summary>
	public class Vrm10Export : IDisposable
	{

		private VRM10ExportSettings m_settings;

		private MeshExportValidator m_meshes;

		private VRM10Object Vrm;

		private ExporterDialogState State;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="asset">出力予定のベースVRMファイル</param>
		public Vrm10Export(GameObject asset)
		{
			State = new ExporterDialogState();

			m_settings = ScriptableObject.CreateInstance<VRM10ExportSettings>();

			m_meshes = ScriptableObject.CreateInstance<MeshExportValidator>();

			State.ExportRoot = asset;
		}

		public void Dispose()
		{
			// Meta
			Vrm = null;
			// m_settings
			ScriptableObject.DestroyImmediate(m_settings);
			m_settings = null;
			// m_meshes
			ScriptableObject.DestroyImmediate(m_meshes);
			m_meshes = null;
		}

		class TmpDisposer : IDisposable
		{
			List<UnityEngine.Object> _disposables = new();
			public void Push(UnityEngine.Object o)
			{
				_disposables.Add(o);
			}

			public void Dispose()
			{
				foreach (var o in _disposables)
				{
					GameObject.DestroyImmediate(o);
				}
				_disposables.Clear();
			}
		}

		/// <summary>
		/// エクスポート直前に事前処理を仕込める
		/// </summary>
		public Action<GameObject> ExportPreLogic { get; set; }

		/// <summary>
		/// 指定されたファイルパス先へVRM1.0をエクスポートする
		/// </summary>
		/// <param name="path">絶対ファイルパス (*.vrm)</param>
		public void Export(string path)
		{

			var root = State.ExportRoot;

			try
			{
				using (var disposer = new TmpDisposer())
				using (var arrayManager = new NativeArrayManager())
				{
					var copy = GameObject.Instantiate(root);
					disposer.Push(copy);
					root = copy;
					ExportPreLogic?.Invoke(root);

					var converter = new UniVRM10.ModelExporter();
#if VRM10_0_128_OR_NEWER
					var model = converter.Export(m_settings.MeshExportSettings, arrayManager, root);
#else
					var model = converter.Export(arrayManager, root);
#endif
					// 右手系に変換
					model.ConvertCoordinate(VrmLib.Coordinates.Vrm1, ignoreVrm: false);

					// export vrm-1.0
					var exporter = new Vrm10Exporter(
						m_settings.MeshExportSettings,
						textureSerializer: new EditorTextureSerializer()
					);
					var option = new VrmLib.ExportArgs
					{
						sparse = m_settings.MorphTargetUseSparse,
					};
					exporter.Export(root, model, converter, option);

					var exportedBytes = exporter.Storage.ToGlbBytes();

					File.WriteAllBytes(path, exportedBytes);
					Debug.Log("exportedBytes: " + exportedBytes.Length);

					var assetPath = UniGLTF.UnityPath.FromFullpath(path);
					if (assetPath.IsUnderWritableFolder)
					{
						// asset folder 内。import を発動
						assetPath.ImportAsset();
					}
				}
			}
			catch (Exception ex)
			{
				// rethrow
				//throw;
				Debug.LogException(ex);
			}
		}
	}

}
