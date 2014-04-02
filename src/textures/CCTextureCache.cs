using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace CocosSharp
{
    public class CCTextureCache : IDisposable, ICCUpdatable
    {
        struct AsyncStruct
        {
			public string  FileName { get; set; }
			public Action<CCTexture2D> Action { get; set; }
        };

		struct DataAsyncStruct
		{
			public byte[] Data { get; set; }
			public string AssetName { get; set; }
			public CCSurfaceFormat Format { get; set; }
			public Action<CCTexture2D> Action { get; set; }
		};

		private List<AsyncStruct> _asyncLoadedImages = new List<AsyncStruct>();
		private List<DataAsyncStruct> _dataAsyncLoadedImages = new List<DataAsyncStruct>();
		private Action ProcessingAction { get; set; }
		private Action ProcessingDataAction { get; set; }
		private object Task { get; set; }

		private static CCTextureCache sharedTextureCache;

        private readonly object m_pDictLock = new object();
        protected Dictionary<string, CCTexture2D> m_pTextures = new Dictionary<string, CCTexture2D>();


        private CCTextureCache()
        {
            ProcessingAction = new Action(
                () =>
                {
                    while (true)
                    {

                        AsyncStruct image;

                        lock (_asyncLoadedImages)
                        {
                            if (_asyncLoadedImages.Count == 0)
                            {
                                Task = null;
                                return;
                            }
                            image = _asyncLoadedImages[0];
                            _asyncLoadedImages.RemoveAt(0);
                        }

                        try
                        {
                            var texture = AddImage(image.FileName);
						CCLog.Log("Loaded texture: {0}", image.FileName);
                            if (image.Action != null)
                            {
                                CCDirector.SharedDirector.Scheduler.Schedule (
                                    f => image.Action(texture), this, 0, 0, 0, false
                                    );
                            }
                        }
                        catch (Exception ex)
                        {
						    CCLog.Log("Failed to load image {0}", image.FileName);
                            CCLog.Log(ex.ToString());
                        }
                    }
                }
                );
			ProcessingDataAction = new Action(
				() =>
				{
					while (true)
					{

						DataAsyncStruct imageData;

						lock (_dataAsyncLoadedImages)
						{
							if (_dataAsyncLoadedImages.Count == 0)
							{
								Task = null;
								return;
							}
							imageData = _dataAsyncLoadedImages[0];
							_dataAsyncLoadedImages.RemoveAt(0);
						}

						try
						{
							var texture = AddImage(imageData.Data, imageData.AssetName, imageData.Format);
							CCLog.Log("Loaded texture: {0}", imageData.AssetName);
							if (imageData.Action != null)
							{
								CCDirector.SharedDirector.Scheduler.Schedule (
									f => imageData.Action(texture), this, 0, 0, 0, false
								);
							}
						}
						catch (Exception ex)
						{
							CCLog.Log("Failed to load image {0}", imageData.AssetName);
							CCLog.Log(ex.ToString());
						}
					}
				}
			);
        }

        public void Update(float dt)
        {
        }

        public static CCTextureCache SharedTextureCache
        {
            get 
            {
                if (sharedTextureCache == null)
                {
                    sharedTextureCache = new CCTextureCache();
                }
                return (sharedTextureCache);
            }
        }

        public static void PurgeSharedTextureCache()
        {
            if (sharedTextureCache != null)
            {
                sharedTextureCache.Dispose();
                sharedTextureCache = null;
            }
        }

        public void UnloadContent()
        {
            m_pTextures.Clear();
        }

        public bool Contains(string assetFile)
        {
            return m_pTextures.ContainsKey(assetFile);
        }

		public void AddImageAsync(byte[] data, string assetName, CCSurfaceFormat format, Action<CCTexture2D> action)
        {
			Debug.Assert(data != null && data.Length != 0, "TextureCache: data MUST not be NULL and MUST contain data");

			lock (_dataAsyncLoadedImages)
            {
				_dataAsyncLoadedImages.Add(new DataAsyncStruct() { Data = data, AssetName = assetName, Format = format  , Action = action});
            }

            if (Task == null)
            {
				Task = CCTask.RunAsync(ProcessingDataAction);
            }
        }

		public void AddImageAsync(string fileimage, Action<CCTexture2D> action)
		{
			Debug.Assert(!String.IsNullOrEmpty(fileimage), "TextureCache: fileimage MUST not be NULL");

			lock (_asyncLoadedImages)
			{
				_asyncLoadedImages.Add(new AsyncStruct() {FileName = fileimage, Action = action});
			}

			if (Task == null)
			{
				Task = CCTask.RunAsync(ProcessingAction);
			}
		}

        public CCTexture2D AddImage(string fileimage)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileimage), "TextureCache: fileimage MUST not be NULL");

			CCTexture2D texture = null;

			var assetName = fileimage;
			if (Path.HasExtension (assetName)) {
				assetName = CCFileUtils.RemoveExtension (assetName);
			}

			lock (m_pDictLock) {
				m_pTextures.TryGetValue (assetName, out texture);
			}
			if (texture == null) {
				texture = new CCTexture2D ();

				if (texture.InitWithFile (fileimage)) {
					lock (m_pDictLock) {
						m_pTextures[assetName] = texture;
					}
				} else {
					return null;
				}
			}
                
			return texture;
		}

		public CCTexture2D AddImage(byte[] data, string assetName, CCSurfaceFormat format)
        {
            lock (m_pDictLock)
            {
                CCTexture2D texture;

                if (!m_pTextures.TryGetValue(assetName, out texture))
                {
                    texture = new CCTexture2D();
                    
                    if (texture.InitWithData(data, format))
                    {
                        m_pTextures.Add(assetName, texture);
                    }
                    else
                    {
                        return null;
                    }
                }
                return texture;
            }
        }

		public CCTexture2D AddRawImage<T>(T[] data, int width, int height, string assetName, CCSurfaceFormat format,
                                          bool premultiplied) where T : struct
        {
            return AddRawImage(data, width, height, assetName, format, premultiplied, false, new CCSize(width, height));
        }

		public CCTexture2D AddRawImage<T>(T[] data, int width, int height, string assetName, CCSurfaceFormat format,
                                          bool premultiplied, bool mipMap) where T : struct
        {
            return AddRawImage(data, width, height, assetName, format, premultiplied, mipMap, new CCSize(width, height));
        }

		public CCTexture2D AddRawImage<T>(T[] data, int width, int height, string assetName, CCSurfaceFormat format,
                                          bool premultiplied, bool mipMap, CCSize contentSize) where T : struct
        {
            CCTexture2D texture;

            lock (m_pDictLock)
            {
                if (!m_pTextures.TryGetValue(assetName, out texture))
                {
                    texture = new CCTexture2D();
                    
					if (texture.InitWithRawData(data, format, width, height, premultiplied, mipMap, contentSize))
                    {
                        m_pTextures.Add(assetName, texture);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return texture;
        }

		public CCTexture2D this[string key]
		{
			get 
			{
				return TextureForKey (key);
			}
		}

        public CCTexture2D TextureForKey(string key)
        {
            CCTexture2D texture = null;
            try
            {
                if (Path.HasExtension(key))
                {
                    key = CCFileUtils.RemoveExtension(key);
                }

                m_pTextures.TryGetValue(key, out texture);
            }
            catch (ArgumentNullException)
            {
                CCLog.Log("Texture of key {0} is not exist.", key);
            }

            return texture;
        }

        public void RemoveAllTextures()
        {
            m_pTextures.Clear();
        }

        public void RemoveUnusedTextures()
        {
            if (m_pTextures.Count > 0)
            {
                var tmp = new Dictionary<string, WeakReference>();

                foreach (var pair in m_pTextures)
                {
                    tmp.Add(pair.Key, new WeakReference(pair.Value));
                }

                m_pTextures.Clear();

                GC.Collect();

                foreach (var pair in tmp)
                {
                    if (pair.Value.IsAlive)
                    {
                        m_pTextures.Add(pair.Key, (CCTexture2D) pair.Value.Target);
                    }
                }
            }
        }

        public void RemoveTexture(CCTexture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            string key = null;

            foreach (var pair in m_pTextures)
            {
                if (pair.Value == texture)
                {
                    key = pair.Key;
                    break;
                }
            }

            if (key != null)
            {
                m_pTextures.Remove(key);
            }
        }

        public void RemoveTextureForKey(string textureKeyName)
        {
            if (String.IsNullOrEmpty(textureKeyName))
            {
                return;
            }

            if (Path.HasExtension(textureKeyName))
            {
                textureKeyName = CCFileUtils.RemoveExtension(textureKeyName);
            }

            m_pTextures.Remove(textureKeyName);
        }

        public void DumpCachedTextureInfo()
        {
            int count = 0;
            int total = 0;

            var copy = m_pTextures.ToList();

            foreach (var pair in copy)
            {
                var texture = pair.Value.XNATexture;

                if (texture != null)
                {
                    var bytes = texture.Width * texture.Height * 4;
                    CCLog.Log("{0} {1} x {2} => {3} KB.", pair.Key, texture.Width, texture.Height, bytes / 1024);
                    total += bytes;
                }

                count++;
            }
            CCLog.Log("{0} textures, for {1} KB ({2:00.00} MB)", count, total / 1024, total / (1024f * 1024f));
        }

		#region Cleaning up

		// No unmanaged resources, so no need for finalizer

        public void Dispose()
        {
			this.Dispose(true);

			GC.SuppressFinalize(this);
        }

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && m_pTextures != null)
			{
				foreach (CCTexture2D t in m_pTextures.Values)
				{
					t.Dispose();
				}

				m_pTextures = null;
			}
		}
    	
		#endregion Cleaning up
	}
}