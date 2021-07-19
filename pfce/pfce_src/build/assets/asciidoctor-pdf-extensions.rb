require 'asciidoctor-pdf' unless defined? ::Asciidoctor::Pdf

module AsciidoctorPdfExtensions
  # Override the built-in layout_toc to move preamble before front of table of contents
  # NOTE we assume that the preamble fits on a single page
  def layout_toc doc, num_levels = 2, toc_page_number = 2, num_front_matter_pages = 0
    go_to_page toc_page_number unless (page_number == toc_page_number) || scratch?
    if scratch?
      preamble = doc.find_by(context: :preamble)
      if (preamble = preamble.first)
        doc.instance_variable_set :@preamble, preamble
        preamble.parent.blocks.delete preamble
      end
    else
      if (preamble = doc.instance_variable_get :@preamble)
        convert_content_for_block preamble
        go_to_page(page_number + 1)
      end
    end
    offset = preamble ? 1 : 0
    toc_page_numbers = super doc, num_levels, (toc_page_number + offset), num_front_matter_pages
    scratch? ? ((toc_page_numbers.begin - offset)..toc_page_numbers.end) : toc_page_numbers
  end
end

Asciidoctor::Pdf::Converter.send(:prepend, AsciidoctorPdfExtensions)
