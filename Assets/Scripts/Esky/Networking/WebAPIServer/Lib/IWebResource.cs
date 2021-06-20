using UnityEngine;
using System.Collections;


namespace BEERLabs.Esky.Networking.WebAPI
{
	public interface IWebResource
	{
        void HandleRequest(Request request, Response response);
	}

}