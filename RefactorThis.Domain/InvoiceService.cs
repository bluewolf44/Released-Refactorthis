using System;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
	public class InvoiceService
	{
		private readonly InvoiceRepository _invoiceRepository;

		public InvoiceService( InvoiceRepository invoiceRepository )
		{
			_invoiceRepository = invoiceRepository;
		}


        //Function to process payment and update inovice linked to payment using 'Reference'. Also save to the database after processing
        //Returns 'String' which is the message of the result of the process
        //Inputs a payment which needs a Reference linking to a Invoice. This invoice needs to have a non-null payments list.
        public string ProcessPayment( Payment payment )
		{
            //Grabbing the intal objects to complete this payment

            //The invoice of the payment. Throws error if can't find it
            var inv = _invoiceRepository.GetInvoice( payment.Reference ) ?? throw new InvalidOperationException( "There is no invoice matching this payment" );
            //The total of all payments in invoice. Throws error if the invoice has null on Payments
            var sumOfPayments = inv.Payments?.Sum(x => x.Amount) ?? throw new InvalidOperationException("Invoice has a null payment list");


            //Checking if the invoice is vaild
            if ( inv.Amount == 0 )
			{
				if (sumOfPayments == 0)
				{
                    return "no payment needed";
				}
				throw new InvalidOperationException( "The invoice is in an invalid state, it has an amount of 0 and it has payments." );
			}

            //Checking for fails in Payment Amount

            //Checking if payment is more than the invoice amount
            if (payment.Amount > inv.Amount)
            {
                return "the payment is greater than the invoice amount";
            }

            //Checking if the invoice is fully paid off
            if (inv.Amount == sumOfPayments)
            {
                return "invoice was already fully paid";
            }

            //Checking if the payment will go over the invoice amount incuiding the amount alreaded paid
            if (payment.Amount > (inv.Amount - inv.AmountPaid))
            {
                return "the payment is greater than the partial amount remaining";
            }

            //Payment & Invoice are confirmed vaild

            //Processing the payment
            switch (inv.Type)
            {
                case InvoiceType.Standard:
                    inv.AmountPaid += payment.Amount;
                    //inv.TaxAmount += payment.Amount * 0.14m; //TODO Not sure if Standard needs tax. Please confirm on code review
                    inv.Payments.Add(payment);
                    break;
                case InvoiceType.Commercial:
                    inv.AmountPaid += payment.Amount;
                    inv.TaxAmount += payment.Amount * 0.14m;
                    inv.Payments.Add(payment);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invoice type can't be processed as not defined");
            }

            //Updated invoice saved to database
            inv.Save();
            //Payment is processed


            //Returning infomation

            //One or more past payments
            if (sumOfPayments != 0)
			{
                if ( inv.Amount == inv.AmountPaid)
				{
                    return "final partial payment received, invoice is now fully paid";
                }
                return "another partial payment received, still not fully paid";
            }

            //First payment
			if (inv.Amount == payment.Amount)
			{
				return "invoice is now fully paid";
			}
            return "invoice is now partially paid";

		}
	}
}