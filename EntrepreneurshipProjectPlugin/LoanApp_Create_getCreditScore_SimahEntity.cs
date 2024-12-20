using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace EntrepreneurshipProjectPlugin
{
    public class LoanApp_Create_getCreditScore_SimahEntity : IPlugin
    {
        //on creation of loan app and preValidation 
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                //throw new NotImplementedException();

                //Obtain the execution context from the service provider.
                IPluginExecutionContext context = (IPluginExecutionContext)
                   serviceProvider.GetService(typeof(IPluginExecutionContext));
                // Obtain the organisation service reference.
                IOrganizationServiceFactory serviceFactory =
                           (IOrganizationServiceFactory)serviceProvider.GetService
                           (typeof(IOrganizationServiceFactory));
                ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                IOrganizationService service =
                           serviceFactory.CreateOrganizationService(context.UserId);
                if (context.PrimaryEntityName == "new_loanapplicatio" && context.Stage == 10 && context.MessageName.Equals("Create"))
                {

                    // The InputParameters collection contains all the data passed in the message request.
                    if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                    {
                        // Obtain the target entity from the input parameters.
                        Entity entity = (Entity)context.InputParameters["Target"];

                        if (entity.Contains("new_email"))
                        {
                            //get the value email from target entity.
                            string Email = entity["new_email"].ToString();

                            //query
                            //Query 1 check the Simah email
                            QueryExpression query = new QueryExpression("new_simah")
                            {
                                ColumnSet = new ColumnSet(true),
                                Criteria = new FilterExpression
                                {
                                    Conditions =
                                    { new ConditionExpression("new_email", ConditionOperator.Equal, Email) }
                                }
                            };
                            EntityCollection result = service.RetrieveMultiple(query);
                            tracingService.Trace("entityCollection =" + result);

                            if (result.Entities.Count > 0)
                            {
                                //Get the value of credit score.
                                Entity simahEntity = result.Entities[0];
                                int creditScore = (int)simahEntity.Attributes["new_creditscore"];

                                //share the value of credit score through shared variable.
                                context.SharedVariables.Add("new_creditscore", creditScore);

                                //check credit score if it is 80 above.
                                if (creditScore < 80)
                                {
                                    //Throw an error if credit score is below 80.
                                    throw new InvalidPluginExecutionException("Sorry,Your credit score is Negative.");
                                }
                                else
                                {
                                    if (entity.Contains("new_stage"))
                                    {
                                        //check if stage value is Interviewcomments.
                                        if (((OptionSetValue)entity["new_stage"]).Value == 100000003)
                                        {
                                            //Query 2 check the Interview Schedule record
                                            QueryExpression qe = new QueryExpression("new_interviewschedule")
                                            {
                                                ColumnSet = new ColumnSet(true),
                                                Criteria = new FilterExpression
                                                {
                                                    Conditions =
                                    { new ConditionExpression("new_emailaddress", ConditionOperator.Equal, Email) }
                                                }
                                            };
                                            EntityCollection InterviewsRecords = service.RetrieveMultiple(qe);
                                            tracingService.Trace("entityCollection =" + InterviewsRecords);

                                            //if there is no record found in interview schedule.
                                            if (InterviewsRecords.Entities.Count == 0)
                                            {
                                                throw new InvalidPluginExecutionException("Please, First schedule an interview for Entrepreneur. ");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //Throw an error if there is no record found for given email.
                                throw new InvalidPluginExecutionException("Sorry,There is no SIMAH record found for the provided Email.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}



