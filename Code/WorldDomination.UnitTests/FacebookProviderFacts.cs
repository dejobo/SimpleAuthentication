﻿using System;
using System.Collections.Specialized;
using System.Net;
using Moq;
using RestSharp;
using WorldDomination.Web.Authentication;
using WorldDomination.Web.Authentication.Facebook;
using Xunit;

namespace WorldDomination.UnitTests
{
    // ReSharper disable InconsistentNaming

    public class FacebookProviderFacts
    {
        public class AuthenticateClientFacts
        {
            [Fact]
            public void
                GivenAValidAccessTokenCallbackButFailsAnCSRFStateCheck_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation
                ()
            {
                // Arrange.
                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(It.IsAny<IRestResponse>);
                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"Code", "aaa"}, {"State", "bbb"}},
                                                        "meh");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal("The states do not match. It's possible that you may be a victim of a CSRF.",
                             result.ErrorInformation.Message);
            }

            [Fact]
            public void
                GivenSomeErrorOccuredWhileTryingToRetrieveAccessToken_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation
                ()
            {
                // Arrange.
                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(It.IsAny<IRestResponse>);
                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result = facebookProvider.AuthenticateClient(new NameValueCollection
                                                                 {
                                                                     {"error_reason", "aaa"},
                                                                     {"error", "bbb"},
                                                                     {"error_description", "ccc"}
                                                                 }, "meh");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal("Reason: aaa. Error: bbb. Description: ccc.",
                             result.ErrorInformation.Message);
            }

            [Fact]
            public void GivenNoValidAccessTokenParams_AuthenticateClient_ReturnsNull()
            {
                // Arrange.
                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(It.IsAny<IRestResponse>);
                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result = facebookProvider.AuthenticateClient(new NameValueCollection {{"aaa", "aaa"},}, "meh");

                // Assert.
                Assert.Null(result);
            }

            [Fact]
            public void
                GivenAnExceptionOccursWhileTryingToRequestAnAccessToken_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation
                ()
            {
                // Arrange.
                const string exceptionMessage = "Some mock exception.";
                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                    .Throws(new Exception(exceptionMessage));
                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"code", "aaa"}, {"state", "bbb"}},
                                                        "bbb");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal("Failed to retrieve an oauth access token from Facebook.", result.ErrorInformation.Message);
                Assert.NotNull(result.ErrorInformation.Exception);
                Assert.IsType<AuthenticationException>(result.ErrorInformation.Exception);
            }

            [Fact]
            public void GivenSomeInvalidRequestToken_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation
                ()
            {
                // Arrange.
                var mockRestResponse = new Mock<IRestResponse>();
                mockRestResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Unauthorized);
                mockRestResponse.Setup(x => x.StatusDescription).Returns("Unauthorised");

                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponse.Object);

                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"code", "aaa"}, {"state", "bbb"}},
                                                        "bbb");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal(
                    "Failed to obtain an Access Token from Facebook OR the the response was not an HTTP Status 200 OK. Response Status: Unauthorized. Response Description: Unauthorised",
                    result.ErrorInformation.Message);
                Assert.NotNull(result.ErrorInformation.Exception);
                Assert.IsType<AuthenticationException>(result.ErrorInformation.Exception);
            }

            [Fact]
            public void GivenAMissingExpiresParam_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation()
            {
                // Arrange.
                var mockRestResponse = new Mock<IRestResponse>();
                mockRestResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
                mockRestResponse.Setup(x => x.Content).Returns("access_token=foo&omg=pewpew");

                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponse.Object);

                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"code", "aaa"}, {"state", "bbb"}},
                                                        "bbb");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal(
                    "Retrieved a Facebook Access Token but it doesn't contain both the access_token and expires_on parameters.",
                    result.ErrorInformation.Message);
                Assert.NotNull(result.ErrorInformation.Exception);
                Assert.IsType<AuthenticationException>(result.ErrorInformation.Exception);
            }

            [Fact]
            public void
                GivenAValidAccessTokenButApiMeThrowsAnException_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation
                ()
            {
                // Arrange.
                const string exceptionMessage = "Some mock exception message.";
                var mockRestResponse = new Mock<IRestResponse>();
                mockRestResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
                mockRestResponse.Setup(x => x.Content).Returns("access_token=foo&expires_on=1000");

                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponse.Object);
                mockRestClient.Setup(x => x.Execute<MeResult>(It.IsAny<IRestRequest>()))
                    .Throws(new Exception(exceptionMessage));

                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);

                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"code", "aaa"}, {"state", "bbb"}},
                                                        "bbb");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal("Failed to retrieve any Me data from the Facebook Api.",
                             result.ErrorInformation.Message);
                Assert.NotNull(result.ErrorInformation.Exception);
                Assert.IsType<AuthenticationException>(result.ErrorInformation.Exception);
            }

            [Fact]
            public void
                GivenAnInvalidMeResultThrowsAnException_AuthenticateClient_ReturnsAnAuthenticatedClientWithErrorInformation
                ()
            {
                // Arrange.
                var mockRestResponse = new Mock<IRestResponse>();
                mockRestResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
                mockRestResponse.Setup(x => x.Content).Returns("access_token=foo&expires_on=1000");

                var mockRestResponseApiMe = new Mock<IRestResponse<MeResult>>();
                mockRestResponseApiMe.Setup(x => x.StatusCode).Returns(HttpStatusCode.Unauthorized);
                mockRestResponseApiMe.Setup(x => x.StatusDescription).Returns("Unauthorized");

                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponse.Object);
                mockRestClient.Setup(x => x.Execute<MeResult>(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponseApiMe.Object);

                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);
                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"code", "aaa"}, {"state", "bbb"}},
                                                        "bbb");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.ErrorInformation);
                Assert.Equal(
                    "Failed to obtain some Me data from the Facebook api OR the the response was not an HTTP Status 200 OK. Response Status: Unauthorized. Response Description: Unauthorized",
                    result.ErrorInformation.Message);
                Assert.NotNull(result.ErrorInformation.Exception);
                Assert.IsType<AuthenticationException>(result.ErrorInformation.Exception);
            }

            [Fact]
            public void GivenValidCredentials_AuthenticateClient_ReturnsAnAuthenticatedClientWithUserInformation()
            {
                // Arrange.
                var mockRestResponseAccessToken = new Mock<IRestResponse>();
                mockRestResponseAccessToken.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
                mockRestResponseAccessToken.Setup(x => x.Content).Returns("access_token=foo&expires_on=1000");

                var meResult = new MeResult
                               {
                                   Id = 1,
                                   FirstName = "some firstname",
                                   LastName = "some lastname",
                                   Link = "http://whatever",
                                   Locale = "en-au",
                                   Name = "Hi there",
                                   Timezone = 10,
                                   Username = "PewPew",
                                   Verified = true
                               };

                var mockRestResponseApiMe = new Mock<IRestResponse<MeResult>>();
                mockRestResponseApiMe.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
                mockRestResponseApiMe.Setup(x => x.Data).Returns(meResult);

                var mockRestClient = new Mock<IRestClient>();
                mockRestClient.Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponseAccessToken.Object);
                mockRestClient.Setup(x => x.Execute<MeResult>(It.IsAny<IRestRequest>()))
                    .Returns(mockRestResponseApiMe.Object);

                var facebookProvider = new FacebookProvider("a", "b", new Uri("http://www.google.com"),
                                                            mockRestClient.Object);
                // Act.
                var result =
                    facebookProvider.AuthenticateClient(new NameValueCollection {{"code", "aaa"}, {"state", "bbb"}},
                                                        "bbb");

                // Assert.
                Assert.NotNull(result);
                Assert.Equal(ProviderType.Facebook, result.ProviderType);
                Assert.NotNull(result.AccessToken);
                Assert.NotNull(result.UserInformation);
                Assert.True(result.UserInformation.Id > 0);
                Assert.NotNull(result.UserInformation.Name);
                Assert.NotNull(result.UserInformation.UserName);
                Assert.Null(result.ErrorInformation);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}