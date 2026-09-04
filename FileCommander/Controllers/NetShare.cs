using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

namespace FileCommander.Controllers;

static class NetShare
{
    public static async Task<T> ExecuteAsync<T>(
       string path,
       Func<Task<T>> operation,
       CancellationToken cancellationToken = default)
    {
        var res = await operation();
        return res;
    }
}
