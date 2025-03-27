﻿namespace Business.Factories.FormValidationFactory.Workers
{
    public class FlowParameterFormValidationWorker : IFormValidationWorker
    {
        public List<string> Validate(object? rawInputValue)
        {
            List<string> errors = new List<string>();
            if (rawInputValue == null || rawInputValue == "")
                errors.Add("Flow Parameter is required!");


            return errors;
        }
    }
}
