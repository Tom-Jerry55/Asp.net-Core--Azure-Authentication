using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KCI.ProjectTracker.AuthServices.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace KCI.ProjectTracker.AuthServices.Controllers
{
    
        [Authorize]
        public class TodoController : Controller
        {
            // GET: /<controller>/
            public async Task<IActionResult> Index()
            {
                AuthenticationResult result = null;
                List<string> itemList = new List<string>();

                try
                {
                    // Because we signed-in already in the WebApp, the userObjectId is know
                    string userObjectID = (User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;

                    // Using ADAL.Net, get a bearer token to access the TodoListService
                    AuthenticationContext authContext = new AuthenticationContext(AzureAdOptions.Settings.Authority, new NaiveSessionCache(userObjectID, HttpContext.Session));
                    ClientCredential credential = new ClientCredential(AzureAdOptions.Settings.ClientId, AzureAdOptions.Settings.ClientSecret);
                    result = await authContext.AcquireTokenSilentAsync(AzureAdOptions.Settings.TodoListResourceId, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                    // Retrieve the user's To Do List.
                    HttpClient client = new HttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, AzureAdOptions.Settings.TodoListBaseAddress + "/api/todolist");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    HttpResponseMessage response = await client.SendAsync(request);

                    // Return the To Do List in the view.
                    if (response.IsSuccessStatusCode)
                    {
                        List<Dictionary<String, String>> responseElements = new List<Dictionary<String, String>>();
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        String responseString = await response.Content.ReadAsStringAsync();
                        responseElements = JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(responseString, settings);
                        foreach (Dictionary<String, String> responseElement in responseElements)
                        {
                           
                            itemList.Add("Hello");
                        }

                        return View();
                    }

                    //
                    // If the call failed with access denied, then drop the current access token from the cache, 
                    //     and show the user an error indicating they might need to sign-in again.
                    //
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                    return View();
                    }
                }
                catch (Exception ex)
                {
                    if (HttpContext.Request.Query["reauth"] == "True")
                    {
                        //
                        // Send an OpenID Connect sign-in request to get a new set of tokens.
                        // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                        // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                        //
                        return new ChallengeResult(OpenIdConnectDefaults.AuthenticationScheme);
                    }
                    //
                    // The user needs to re-authorize.  Show them a message to that effect.
                    //
                   // TodoItem newItem = new TodoItem();
                    //newItem.Title = "(Sign-in required to view to do list.)";
                    //itemList.Add(newItem);
                    //ViewBag.ErrorMessage = "AuthorizationRequired";
                    return View();
                }
                //
                // If the call failed for any other reason, show the user an error.
                //
                return View("Error");
            }

            [HttpPost]
            public async Task<ActionResult> Index(string item)
            {
                if (ModelState.IsValid)
                {
                    //
                    // Retrieve the user's tenantID and access token since they are parameters used to call the To Do service.
                    //
                    AuthenticationResult result = null;
                    List<string> itemList = new List<string>();

                    try
                    {
                        string userObjectID = (User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
                        AuthenticationContext authContext = new AuthenticationContext(AzureAdOptions.Settings.Authority, new NaiveSessionCache(userObjectID, HttpContext.Session));
                        ClientCredential credential = new ClientCredential(AzureAdOptions.Settings.ClientId, AzureAdOptions.Settings.ClientSecret);
                        result = await authContext.AcquireTokenSilentAsync(AzureAdOptions.Settings.TodoListResourceId, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                        // Forms encode todo item, to POST to the todo list web api.
                        HttpContent content = new StringContent(JsonConvert.SerializeObject(new { Title = item }), System.Text.Encoding.UTF8, "application/json");

                        //
                        // Add the item to user's To Do List.
                        //
                        HttpClient client = new HttpClient();
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, AzureAdOptions.Settings.TodoListBaseAddress + "/api/todolist");
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                        request.Content = content;
                        HttpResponseMessage response = await client.SendAsync(request);

                        //
                        // Return the To Do List in the view.
                        //
                        if (response.IsSuccessStatusCode)
                        {
                            return RedirectToAction("Index");
                        }

                        //
                        // If the call failed with access denied, then drop the current access token from the cache, 
                        //     and show the user an error indicating they might need to sign-in again.
                        //
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                        return View();
                        }
                    }
                    catch (Exception)
                    {
                        //
                        // The user needs to re-authorize.  Show them a message to that effect.
                        //
                       
                      
                        return View(itemList);
                    }
                    //
                    // If the call failed for any other reason, show the user an error.
                    //
                    return View("Error");
                }
                return View("Error");
            }

         
        }
    }
