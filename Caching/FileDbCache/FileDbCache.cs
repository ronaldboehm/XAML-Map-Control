﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;
using FileDbNs;

namespace Caching
{
    /// <summary>
    /// ObjectCache implementation based on EzTools FileDb - http://www.eztools-software.com/tools/filedb/.
    /// </summary>
    public class FileDbCache : ObjectCache, IDisposable
    {
        private const string keyField = "Key";
        private const string valueField = "Value";
        private const string expiresField = "Expires";

        private readonly BinaryFormatter formatter = new BinaryFormatter();
        private readonly FileDb fileDb = new FileDb();
        private readonly string name;
        private readonly string path;

        public FileDbCache(string name, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The parameter path must not be null or empty.");
            }

            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path += ".fdb";
            }

            this.name = name;
            this.path = path;

            try
            {
                fileDb.Open(path, false);
            }
            catch
            {
                CreateDatebase();
            }

            Trace.TraceInformation("FileDbCache created with {0} cached items", fileDb.NumRecords);
        }

        public bool AutoFlush
        {
            get { return fileDb.AutoFlush; }
            set { fileDb.AutoFlush = value; }
        }

        public int AutoCleanThreshold
        {
            get { return fileDb.AutoCleanThreshold; }
            set { fileDb.AutoCleanThreshold = value; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get { return DefaultCacheCapabilities.InMemoryProvider | DefaultCacheCapabilities.AbsoluteExpirations | DefaultCacheCapabilities.SlidingExpirations; }
        }

        public override object this[string key]
        {
            get { return Get(key); }
            set { Set(key, value, null); }
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotSupportedException("FileDbCache does not support the ability to enumerate items.");
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotSupportedException("FileDbCache does not support the ability to create change monitors.");
        }

        public override long GetCount(string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException("The parameter regionName must be null.");
            }

            long count = 0;

            try
            {
                count = fileDb.NumRecords;
            }
            catch
            {
                if (CheckReindex())
                {
                    try
                    {
                        count = fileDb.NumRecords;
                    }
                    catch
                    {
                        CreateDatebase();
                    }
                }
            }

            return count;
        }

        public override bool Contains(string key, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException("The parameter regionName must be null.");
            }

            if (key == null)
            {
                throw new ArgumentNullException("The parameter key must not be null.");
            }

            bool contains = false;

            try
            {
                contains = fileDb.GetRecordByKey(key, new string[0], false) != null;
            }
            catch
            {
                if (CheckReindex())
                {
                    try
                    {
                        contains = fileDb.GetRecordByKey(key, new string[0], false) != null;
                    }
                    catch
                    {
                        CreateDatebase();
                    }
                }
            }

            return contains;
        }

        public override object Get(string key, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException("The parameter regionName must be null.");
            }

            if (key == null)
            {
                throw new ArgumentNullException("The parameter key must not be null.");
            }

            object value = null;
            Record record = null;

            try
            {
                record = fileDb.GetRecordByKey(key, new string[] { valueField }, false);
            }
            catch
            {
                if (CheckReindex())
                {
                    try
                    {
                        record = fileDb.GetRecordByKey(key, new string[] { valueField }, false);
                    }
                    catch
                    {
                        CreateDatebase();
                    }
                }
            }

            if (record != null)
            {
                try
                {
                    using (MemoryStream stream = new MemoryStream((byte[])record[0]))
                    {
                        value = formatter.Deserialize(stream);
                    }
                }
                catch (Exception exc)
                {
                    Trace.TraceWarning("FileDbCache.Get({0}): {1}", key, exc.Message);

                    try
                    {
                        fileDb.DeleteRecordByKey(key);
                    }
                    catch
                    {
                    }
                }
            }

            return value;
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            var value = Get(key, regionName);
            return value != null ? new CacheItem(key, value) : null;
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException("The parameter regionName must be null.");
            }

            var values = new Dictionary<string, object>();

            foreach (string key in keys)
            {
                values[key] = Get(key);
            }

            return values;
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            if (regionName != null)
            {
                throw new NotSupportedException("The parameter regionName must be null.");
            }

            if (value == null)
            {
                throw new ArgumentNullException("The parameter value must not be null.");
            }

            if (key == null)
            {
                throw new ArgumentNullException("The parameter key must not be null.");
            }

            byte[] valueBuffer = null;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, value);
                    valueBuffer = stream.ToArray();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("FileDbCache.Set({0}): {1}", key, exc.Message);
            }

            if (valueBuffer != null)
            {
                DateTime expires = DateTime.MaxValue;

                if (policy.AbsoluteExpiration != InfiniteAbsoluteExpiration)
                {
                    expires = policy.AbsoluteExpiration.DateTime;
                }
                else if (policy.SlidingExpiration != NoSlidingExpiration)
                {
                    expires = DateTime.UtcNow + policy.SlidingExpiration;
                }

                try
                {
                    AddOrUpdateRecord(key, valueBuffer, expires);
                }
                catch
                {
                    if (CheckReindex())
                    {
                        try
                        {
                            AddOrUpdateRecord(key, valueBuffer, expires);
                        }
                        catch
                        {
                            CreateDatebase();
                            AddOrUpdateRecord(key, valueBuffer, expires);
                        }
                    }
                }
            }
        }

        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            Set(item.Key, item.Value, policy, item.RegionName);
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            Set(key, value, new CacheItemPolicy { AbsoluteExpiration = absoluteExpiration }, regionName);
        }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            var oldValue = Get(key, regionName);
            Set(key, value, policy);
            return oldValue;
        }

        public override CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy)
        {
            var oldItem = GetCacheItem(item.Key, item.RegionName);
            Set(item, policy);
            return oldItem;
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            return AddOrGetExisting(key, value, new CacheItemPolicy { AbsoluteExpiration = absoluteExpiration }, regionName);
        }

        public override object Remove(string key, string regionName = null)
        {
            var oldValue = Get(key, regionName);

            if (oldValue != null)
            {
                try
                {
                    fileDb.DeleteRecordByKey(key);
                }
                catch
                {
                }
            }

            return oldValue;
        }

        public void Flush()
        {
            try
            {
                fileDb.Flush();
            }
            catch
            {
                CheckReindex();
            }
        }

        public void Clean()
        {
            try
            {
                fileDb.Clean();
            }
            catch
            {
                CheckReindex();
            }
        }

        public void Dispose()
        {
            try
            {
                fileDb.DeleteRecords(new FilterExpression(expiresField, DateTime.UtcNow, EqualityEnum.LessThanOrEqual));
                Trace.TraceInformation("FileDbCache has deleted {0} expired items", fileDb.NumDeleted);
                fileDb.Clean();
                fileDb.Close();
            }
            catch
            {
                if (CheckReindex())
                {
                    fileDb.Close();
                }
            }
        }

        private bool CheckReindex()
        {
            if (fileDb.IsOpen)
            {
                Trace.TraceWarning("FileDbCache is reindexing the cache database");
                fileDb.Reindex();
                return true;
            }

            return false;
        }

        private void CreateDatebase()
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            fileDb.Create(path, new Field[]
                {
                    new Field(keyField, DataTypeEnum.String) { IsPrimaryKey = true },
                    new Field(valueField, DataTypeEnum.Byte) { IsArray = true },
                    new Field(expiresField, DataTypeEnum.DateTime)
                });
        }

        private void AddOrUpdateRecord(string key, object value, DateTime expires)
        {
            var fieldValues = new FieldValues(3); // capacity
            fieldValues.Add(valueField, value);
            fieldValues.Add(expiresField, expires);

            if (fileDb.GetRecordByKey(key, new string[0], false) == null)
            {
                fieldValues.Add(keyField, key);
                fileDb.AddRecord(fieldValues);
            }
            else
            {
                fileDb.UpdateRecordByKey(key, fieldValues);
            }
        }
    }
}