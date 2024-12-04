using System;



namespace MTCG.Server
{
    /// <summary>This delegate is used for <see cref="HttpServer"/> events.</summary>
    /// <param name="sender">Sending object.</param>
    /// <param name="e">Event arguments.</param>
    public delegate void HttpSvrEventHandler(object sender, HttpSvrEventArgs e);
}