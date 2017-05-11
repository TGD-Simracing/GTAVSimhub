﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mock.Plugin
{

    sealed class DataProducer : IDisposable
    {       
        private BinaryFormatter binaryFormatter = new BinaryFormatter();
        private SharedMemory.SharedArray<byte> sharedBuffer = null;

        string dataRow(string property, object value)
        {
            string type;

            type = value.GetType().Name;
            string r = property + ":" + type + ":" + value.ToString();
            return r;
        }

        private byte[] ToBinary(Object source)
        {
            using (var ms = new MemoryStream())
            {
                binaryFormatter.Serialize(ms, source);
                ms.Flush();
                return ms.ToArray();
            }
        }

        public void Share(Object o)
        {
            byte[] rawData = ToBinary(o);
            int dataSize = rawData.Length;

            // Write the dataSize and the rawData into the shared buffer
            Byte[] buf = new Byte[4 + dataSize];
            Array.Copy(BitConverter.GetBytes(dataSize), buf, 4);
            Array.Copy(rawData, 0, buf, 4, rawData.Length);

            try
            {
                // Acquire the write lock
                sharedBuffer.AcquireWriteLock();
                // Write binary data
                sharedBuffer.Write(buf);
                // Release the write lock
                sharedBuffer.ReleaseWriteLock();
            }
            catch (TimeoutException e)
            {
                Console.Write(e.Message);
            }
        }

        // Class constructor
        public DataProducer(string memId)
        {
            try
            {
                sharedBuffer = new SharedMemory.SharedArray<byte>(name: memId);
            }
            catch (Exception e)
            {
                sharedBuffer = new SharedMemory.SharedArray<byte>(name: memId, length: 65535);
            }
        }

        static void Main(string[] args)
        {
            //binaryFormatter.Binder = new CurrentAssemblyDeserializationBinder();
            var producer = new DataProducer("GTAV");


            for (;;)
            {
                List<string> data = new List<string>();

                data.Add(producer.dataRow("Name", "Carlo"));
                data.Add(producer.dataRow("Speed", 3.4343));
                data.Add(producer.dataRow("Gear", 1));
                data.Add(producer.dataRow("RPM", 133));

                producer.Share(data.ToArray());
                Thread.Sleep(10000);                           
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // Per rilevare chiamate ridondanti

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminare lo stato gestito (oggetti gestiti).
                    sharedBuffer.Dispose();
                }

                // TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire sotto l'override di un finalizzatore.
                // TODO: impostare campi di grandi dimensioni su Null.

                disposedValue = true;
            }
        }

        // TODO: eseguire l'override di un finalizzatore solo se Dispose(bool disposing) include il codice per liberare risorse non gestite.
        // ~Producer() {
        //   // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
        //   Dispose(false);
        // }

        // Questo codice viene aggiunto per implementare in modo corretto il criterio Disposable.
        public void Dispose()
        {
            // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
            Dispose(true);
            // TODO: rimuovere il commento dalla riga seguente se è stato eseguito l'override del finalizzatore.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
