using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomSpeechModels
{
    internal class Program
    {
        private static string key = "KEYHERE";
        private static string eastregion = "eastus";
        private static string westregion = "westus";
        private static readonly string Endpoint = "https://" + eastregion + ".api.cognitive.microsoft.com";

        private const string baseModel = "a41b262e-3c75-46cf-ba0a-b24382e3a23f";

        private static string locale = "en-US";

        
        public static async Task Main(string[] args)
        {
            /*await RemoveAllEntities();
            
            string dataset = await UploadDataset();
            string model = await CreateModel(baseModel, dataset);
            string endpoint = await CreateEndpoint(model, "11 phrases endpoint");*/
            
            var endpoint = (await GetFullEndpoints())[0];

            await UseEndpoint(endpoint, "unmute.wav");
            await UseEndpoint(endpoint, "mute.wav");
            await UseEndpoint(endpoint, "call.wav");
            await UseEndpoint(endpoint, "repeat.wav");
        }
        
        // dataset, model, endpoint
        static async Task RemoveAllEntities()
        {
            var datasets = await GetEntities("datasets");
            var models = await GetEntities("models");
            var endpoints = await GetEntities("endpoints");

            var baseModels = await GetEntities("models/base");
            var projects = await GetEntities("projects");

            foreach (var dataset in datasets)
            {
                await DeleteEntity("datasets", dataset);
            }
            
            //endpoint should be deleted before model, because otherwise there'll be 400 badrequest
            foreach (var endpoint in endpoints)
            {
                await DeleteEntity("endpoints", endpoint);
            }
            
            foreach (var model in models)
            {
                await DeleteEntity("models", model);
            }
        }
        static async Task<string> UploadDataset()
        {
            var contenturl = "https://github.com/andrewandrey5266/azure-cs/blob/main/format1.zip?raw=true";
            var url = "https://" + eastregion + ".api.cognitive.microsoft.com/speechtotext/v3.0/datasets";
            //var project = "https://westus.api.cognitive.microsoft.com/speechtotext/v3.0/projects/00fdcaec-d6ff-49b9-9f1b-d194baa3841b";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                var serialized = JsonConvert.SerializeObject(new
                {
                    kind = "Acoustic",
                    contentUrl = contenturl,
                    locale = locale,
                    displayName = "11-phrases-dataset",
                    description = "11 phrases provided by Gary",
                });
                var response = await client.PostAsync(
                    url,
                    new CapturedJsonContent(serialized));

                var content = await response.Content.ReadAsStringAsync();
                return JObject.Parse(content)["self"].GetSelfId();
            }
        }

        static async Task<string> CreateModel(string baseModel, string dataset)
        { 
            string url = Endpoint + "/speechtotext/v3.0/models";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                    var serialized = JsonConvert.SerializeObject(
                        new
                        {
                            baseModel = new
                            {
                                self =
                                    "https://eastus.api.cognitive.microsoft.com/speechtotext/v3.0/models/base/" + baseModel
                            },
                            datasets = new[]
                            {

                                new
                                {
                                    self =
                                        "https://eastus.api.cognitive.microsoft.com/speechtotext/v3.0/datasets/" + dataset
                                }

                            },
                            locale = locale,
                            displayName = "Model with 11 datasets"
                        }
                    );

                    var response = await client.PostAsync(url, new CapturedJsonContent(serialized));
                    
                    var content = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(content)["self"].GetSelfId();
               
            }
            
        }

        static async Task<string> CreateEndpoint(string model, string displayName)
        {
            string url = Endpoint + "/speechtotext/v3.0/endpoints";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                var serialized = JsonConvert.SerializeObject(
                    new
                    {
                        model = new
                        {
                            self =
                                "https://eastus.api.cognitive.microsoft.com/speechtotext/v3.0/models/" + model
                        },
                        properties = new
                        {
                                loggingEnabled = true
                        },
                        locale = locale,
                        displayName = displayName,
                        description = "This is a speech endpoint"
                    }
                );

                var response = await client.PostAsync(url, new CapturedJsonContent(serialized));
                var content = await response.Content.ReadAsStringAsync();
                    
                var deserializeObject = JObject.Parse(content);
                return deserializeObject["links"]?["restConversation"]?.ToString();
            }
        }

        static async Task UseEndpoint(string endpoint, string fileName)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                    byte[] bytes = File.ReadAllBytes($"D:\\{fileName}");

                    var response = await client.PostAsync(endpoint, new ByteArrayContent(bytes));
                    var content = await response.Content.ReadAsStringAsync();
                    
                    var deserializeObject = JObject.Parse(content);
            }
        }
        
        static async Task DeleteEntity(string entity, string id)
        {
            string url =  $"{Endpoint}/speechtotext/v3.0/{entity}/{id}";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                await client.DeleteAsync(url);
            }
        }

        static async Task<List<string>> GetFullEndpoints()
        {
            string url = $"https://{eastregion}.api.cognitive.microsoft.com/speechtotext/v3.0/endpoints";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                if (content != null)
                {
                    return JObject.Parse(content)["values"].Select(x => x["links"]?["restConversation"]?.ToString()).ToList();
                }
            }

            return null;
        }
        
        static async Task<List<string>> GetEntities(string entity)
        {
            string url = $"https://{eastregion}.api.cognitive.microsoft.com/speechtotext/v3.0/{entity}";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                if (content != null)
                {
                    return JObject.Parse(content)["values"].Select(x => x["self"].GetSelfId()).ToList();
                }
            }

            return null;
        }

    }
    public static class JTokenHelper{
       public static string GetSelfId(this JToken self) => self.ToString().Split('/').Last();
    }
}
