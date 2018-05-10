using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PgpCore;

namespace PgpFunction
{
    public static class PGPEncrypt
    {
        [FunctionName("PGPEncrypt")]
        public static void Run(
            [BlobTrigger("input/{name}", Connection = "BlobConnectionString")] Stream myBlob,
            [Blob("output/{name}.pgp", FileAccess.Write)] Stream outputBlob,
            string name, 
            TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var publicKeyBase64 = ConfigurationManager.AppSettings["publicKeyBase64"];

            byte[] data = Convert.FromBase64String(publicKeyBase64);
            string publicKey = Encoding.UTF8.GetString(data);
            Encrypt(myBlob, outputBlob, publicKey);

            DeleteInput("input", name);
        }

        private static void DeleteInput(string containerName, string fileName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["BlobConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(fileName);
            blockBlob.DeleteIfExists();
        }

        private static void Encrypt(Stream inputStream, Stream outputStream, string publicKey)
        {
            using (PGP pgp = new PGP())
            {
                using (inputStream)
                using (Stream publicKeyStream = GenerateStreamFromString(publicKey))
                {
                    pgp.EncryptStream(inputStream, outputStream, publicKeyStream, true, true);
                }
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
