﻿using Microsoft.Extensions.Configuration;
using Tweed.Data.GenerateFakes;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
    .Build();

using var store = RavenDocumentStore.OpenDocumentStore(config);

await using var bulkInsert = store.BulkInsert();

var identityUsers = await FakesCreator.CreateFakeIdentityUsers(config, bulkInsert);
var tweedUsers = await FakesCreator.CreateFakeTweedUsers(config, bulkInsert, identityUsers);

using (var session = store.OpenAsyncSession())
{
    await FakesCreator.CreateFakeFollows(tweedUsers, session);

    await session.SaveChangesAsync();
}

Console.WriteLine("{0} AppUser instances updated with followers", identityUsers.Count);

var tweeds = await FakesCreator.CreateTweeds(identityUsers, config, bulkInsert);

using (var session = store.OpenAsyncSession())
{
    await FakesCreator.CreateLikes(tweeds, tweedUsers, session);

    await session.SaveChangesAsync();
}

Console.WriteLine("{0} AppUser instances updated with likes", identityUsers.Count);

