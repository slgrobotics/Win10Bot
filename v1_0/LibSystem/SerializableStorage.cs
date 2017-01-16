/*
 * Copyright (c) 2016..., Sergei Grichine   http://trackroamer.com
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *    
 * this is a no-warranty no-liability permissive license - you do not have to publish your changes,
 * although doing so, donating and contributing is always appreciated
 */

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;

namespace slg.LibSystem
{
    /// <summary>
    /// Provides functions to save and load single object as well as List of 'T' using serialization
    /// from http://stackoverflow.com/questions/34385625/saving-files-on-raspberry-pi-with-windows-iot
    /// </summary>
    /// <typeparam name="T">Type parameter to be serialize</typeparam>
    public static class SerializableStorage<T> where T : new()
    {
        public static async void Save(string FileName, T _Data)
        {
            MemoryStream _MemoryStream = new MemoryStream();

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            XmlWriter writer = XmlDictionaryWriter.Create(_MemoryStream, xmlWriterSettings);

            DataContractSerializer Serializer = new DataContractSerializer(typeof(T));
            Serializer.WriteObject(writer, _Data);
            writer.Flush();

            StorageFile _File = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);

            using (Stream fileStream = await _File.OpenStreamForWriteAsync())
            {
                _MemoryStream.Seek(0, SeekOrigin.Begin);
                await _MemoryStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Dispose();
            }
        }

        public static async Task<T> Load(string FileName)
        {
            StorageFolder _Folder = ApplicationData.Current.LocalFolder;
            StorageFile _File;
            T Result;

            try
            {
                Task.WaitAll();
                _File = await _Folder.GetFileAsync(FileName);

                using (Stream stream = await _File.OpenStreamForReadAsync())
                {
                    DataContractSerializer Serializer = new DataContractSerializer(typeof(T));

                    Result = (T)Serializer.ReadObject(stream);

                }
                return Result;
            }
            catch (Exception ex)
            {
                return new T();
            }
        }
    }
}
