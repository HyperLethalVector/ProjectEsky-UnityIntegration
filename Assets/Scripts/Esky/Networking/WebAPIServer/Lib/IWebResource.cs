using UnityEngine;
using System.Collections;


namespace BEERLabs.ProjectEsky.Networking.WebAPI
{
	public interface IWebResource
	{
        void HandleRequest(Request request, Response response);
	}

}